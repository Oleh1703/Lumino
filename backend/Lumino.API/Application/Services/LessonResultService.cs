using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;

namespace Lumino.Api.Application.Services
{
    public class LessonResultService : ILessonResultService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IAchievementService _achievementService;

        public LessonResultService(LuminoDbContext dbContext, IAchievementService achievementService)
        {
            _dbContext = dbContext;
            _achievementService = achievementService;
        }

        public SubmitLessonResponse SubmitLesson(int userId, SubmitLessonRequest request)
        {
            var lesson = _dbContext.Lessons.First(x => x.Id == request.LessonId);

            var exercises = _dbContext.Exercises
                .Where(x => x.LessonId == lesson.Id)
                .ToList();

            int correct = 0;

            foreach (var exercise in exercises)
            {
                var userAnswer = request.Answers
                    .FirstOrDefault(x => x.ExerciseId == exercise.Id);

                if (userAnswer != null &&
                    userAnswer.Answer.Trim().ToLower() ==
                    exercise.CorrectAnswer.Trim().ToLower())
                {
                    correct++;
                }
            }

            var result = new LessonResult
            {
                UserId = userId,
                LessonId = lesson.Id,
                Score = correct,
                TotalQuestions = exercises.Count,
                CompletedAt = DateTime.UtcNow
            };

            _dbContext.LessonResults.Add(result);
            _dbContext.SaveChanges();

            UpdateUserProgress(userId, correct);
            _achievementService.CheckAndGrantAchievements(userId, correct, exercises.Count);

            return new SubmitLessonResponse
            {
                TotalExercises = exercises.Count,
                CorrectAnswers = correct,
                IsPassed = correct == exercises.Count
            };
        }

        private void UpdateUserProgress(int userId, int score)
        {
            var progress = _dbContext.UserProgresses
                .FirstOrDefault(x => x.UserId == userId);

            if (progress == null)
            {
                progress = new UserProgress
                {
                    UserId = userId,
                    CompletedLessons = 1,
                    TotalScore = score,
                    LastUpdatedAt = DateTime.UtcNow
                };

                _dbContext.UserProgresses.Add(progress);
            }
            else
            {
                progress.CompletedLessons++;
                progress.TotalScore += score;
                progress.LastUpdatedAt = DateTime.UtcNow;
            }

            _dbContext.SaveChanges();
        }
    }
}

