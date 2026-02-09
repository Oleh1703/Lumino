using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using System.Text.Json;
using System.Collections.Generic;

namespace Lumino.Api.Application.Services
{
    public class LessonResultService : ILessonResultService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IAchievementService _achievementService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ISubmitLessonRequestValidator _submitLessonRequestValidator;

        public LessonResultService(
            LuminoDbContext dbContext,
            IAchievementService achievementService,
            IDateTimeProvider dateTimeProvider,
            ISubmitLessonRequestValidator submitLessonRequestValidator)
        {
            _dbContext = dbContext;
            _achievementService = achievementService;
            _dateTimeProvider = dateTimeProvider;
            _submitLessonRequestValidator = submitLessonRequestValidator;
        }

        public SubmitLessonResponse SubmitLesson(int userId, SubmitLessonRequest request)
        {
            _submitLessonRequestValidator.Validate(request);

            var lesson = _dbContext.Lessons.FirstOrDefault(x => x.Id == request.LessonId);

            if (lesson == null)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            var exercises = _dbContext.Exercises
                .Where(x => x.LessonId == lesson.Id)
                .ToList();

            int correct = 0;
            var mistakeExerciseIds = new List<int>();

            foreach (var exercise in exercises)
            {
                var userAnswer = request.Answers
                    .FirstOrDefault(x => x.ExerciseId == exercise.Id);

                var isCorrect = userAnswer != null &&
                    !string.IsNullOrWhiteSpace(userAnswer.Answer) &&
                    userAnswer.Answer.Trim().ToLower() ==
                    exercise.CorrectAnswer.Trim().ToLower();

                if (isCorrect)
                {
                    correct++;
                    continue;
                }

                mistakeExerciseIds.Add(exercise.Id);
            }

            var shouldIncrementCompletedLessons = !_dbContext.LessonResults.Any(x => x.UserId == userId && x.LessonId == lesson.Id);

            var result = new LessonResult
            {
                UserId = userId,
                LessonId = lesson.Id,
                Score = correct,
                TotalQuestions = exercises.Count,
                MistakesJson = JsonSerializer.Serialize(mistakeExerciseIds),
                CompletedAt = _dateTimeProvider.UtcNow
            };

            _dbContext.LessonResults.Add(result);
            _dbContext.SaveChanges();

            UpdateUserProgress(userId, correct, shouldIncrementCompletedLessons);
            _achievementService.CheckAndGrantAchievements(userId, correct, exercises.Count);

            return new SubmitLessonResponse
            {
                TotalExercises = exercises.Count,
                CorrectAnswers = correct,
                IsPassed = correct == exercises.Count,
                MistakeExerciseIds = mistakeExerciseIds
            };
        }

        private void UpdateUserProgress(int userId, int score, bool shouldIncrementCompletedLessons)
        {
            var progress = _dbContext.UserProgresses
                .FirstOrDefault(x => x.UserId == userId);

            if (progress == null)
            {
                progress = new UserProgress
                {
                    UserId = userId,
                    CompletedLessons = shouldIncrementCompletedLessons ? 1 : 0,
                    TotalScore = score,
                    LastUpdatedAt = _dateTimeProvider.UtcNow
                };

                _dbContext.UserProgresses.Add(progress);
            }
            else
            {
                if (shouldIncrementCompletedLessons)
                {
                    progress.CompletedLessons++;
                }

                progress.TotalScore += score;
                progress.LastUpdatedAt = _dateTimeProvider.UtcNow;
            }

            _dbContext.SaveChanges();
        }
    }
}
