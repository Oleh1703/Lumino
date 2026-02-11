using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
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
        private readonly LearningSettings _learningSettings;

        public LessonResultService(
            LuminoDbContext dbContext,
            IAchievementService achievementService,
            IDateTimeProvider dateTimeProvider,
            ISubmitLessonRequestValidator submitLessonRequestValidator,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _achievementService = achievementService;
            _dateTimeProvider = dateTimeProvider;
            _submitLessonRequestValidator = submitLessonRequestValidator;
            _learningSettings = learningSettings.Value;
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
            var answers = new List<LessonAnswerResultDto>();

            foreach (var exercise in exercises)
            {
                var userAnswer = request.Answers
                    .FirstOrDefault(x => x.ExerciseId == exercise.Id);

                var userAnswerText = userAnswer != null
                    ? (userAnswer.Answer ?? string.Empty)
                    : string.Empty;

                var isCorrect = !string.IsNullOrWhiteSpace(userAnswerText) &&
                    userAnswerText.Trim().ToLower() ==
                    exercise.CorrectAnswer.Trim().ToLower();

                answers.Add(new LessonAnswerResultDto
                {
                    ExerciseId = exercise.Id,
                    UserAnswer = userAnswerText,
                    CorrectAnswer = exercise.CorrectAnswer,
                    IsCorrect = isCorrect
                });

                if (isCorrect)
                {
                    correct++;
                    continue;
                }

                mistakeExerciseIds.Add(exercise.Id);
            }

            var passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);
            var isPassed = LessonPassingRules.IsPassed(correct, exercises.Count, passingScorePercent);

            // CompletedLessons збільшуємо лише при першому PASSED цього уроку
            var shouldIncrementCompletedLessons = isPassed &&
                !_dbContext.LessonResults.Any(x =>
                    x.UserId == userId &&
                    x.LessonId == lesson.Id &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                );

            // зберігаємо деталізацію в MistakesJson (backward-compatible)
            var detailsJson = new LessonResultDetailsJson
            {
                MistakeExerciseIds = mistakeExerciseIds,
                Answers = answers
            };

            var result = new LessonResult
            {
                UserId = userId,
                LessonId = lesson.Id,
                Score = correct,
                TotalQuestions = exercises.Count,
                MistakesJson = JsonSerializer.Serialize(detailsJson),
                CompletedAt = _dateTimeProvider.UtcNow
            };

            _dbContext.LessonResults.Add(result);
            _dbContext.SaveChanges();

            UpdateUserProgress(userId, shouldIncrementCompletedLessons);
            _achievementService.CheckAndGrantAchievements(userId, correct, exercises.Count);

            return new SubmitLessonResponse
            {
                TotalExercises = exercises.Count,
                CorrectAnswers = correct,
                IsPassed = isPassed,
                MistakeExerciseIds = mistakeExerciseIds,
                Answers = answers
            };
        }

        private void UpdateUserProgress(int userId, bool shouldIncrementCompletedLessons)
        {
            var progress = _dbContext.UserProgresses
                .FirstOrDefault(x => x.UserId == userId);

            if (progress == null)
            {
                progress = new UserProgress
                {
                    UserId = userId,
                    CompletedLessons = shouldIncrementCompletedLessons ? 1 : 0,
                    TotalScore = CalculateBestTotalScore(userId),
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

                // TotalScore = сума найкращих результатів по кожному уроку
                progress.TotalScore = CalculateBestTotalScore(userId);
                progress.LastUpdatedAt = _dateTimeProvider.UtcNow;
            }

            _dbContext.SaveChanges();
        }

        private int CalculateBestTotalScore(int userId)
        {
            return _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .GroupBy(x => x.LessonId)
                .Select(g => g.Max(x => x.Score))
                .Sum();
        }
    }
}
