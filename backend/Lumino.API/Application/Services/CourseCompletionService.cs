using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using System;

namespace Lumino.Api.Application.Services
{
    public class CourseCompletionService : ICourseCompletionService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly LearningSettings _learningSettings;

        public CourseCompletionService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _learningSettings = learningSettings.Value;
        }

        public CourseCompletionResponse GetMyCourseCompletion(int userId, int courseId)
        {
            var course = _dbContext.Courses
                .FirstOrDefault(x => x.Id == courseId && x.IsPublished);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            var orderedLessonIds = (
                from t in _dbContext.Topics
                join l in _dbContext.Lessons on t.Id equals l.TopicId
                where t.CourseId == courseId
                orderby t.Order, l.Order
                select l.Id
            ).ToList();

            if (orderedLessonIds.Count == 0)
            {
                throw new KeyNotFoundException("Course has no lessons");
            }

            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            var passedLessonIds = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    orderedLessonIds.Contains(x.LessonId) &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .Select(x => x.LessonId)
                .Distinct()
                .ToList();

            var remaining = orderedLessonIds
                .Where(x => !passedLessonIds.Contains(x))
                .ToList();

            int totalLessons = orderedLessonIds.Count;
            int completedLessons = passedLessonIds.Count;

            int percent = 0;

            if (totalLessons > 0 && completedLessons > 0)
            {
                percent = (int)Math.Round((double)completedLessons * 100 / totalLessons);
            }

            // --- scenes ---
            int scenesTotal = 0;
            int scenesCompleted = 0;
            int scenesPercent = 0;
            bool scenesIncluded = false;

            var allSceneIds = _dbContext.Scenes
                .Select(x => x.Id)
                .OrderBy(x => x)
                .ToList();

            if (allSceneIds.Count > 0)
            {
                var unlockedSceneIds = allSceneIds
                    .Where(id => SceneUnlockRules.IsUnlocked(id, completedLessons, _learningSettings.SceneUnlockEveryLessons))
                    .ToList();

                scenesTotal = unlockedSceneIds.Count;

                if (scenesTotal > 0)
                {
                    scenesIncluded = true;

                    scenesCompleted = _dbContext.SceneAttempts
                        .Where(x => x.UserId == userId && x.IsCompleted && unlockedSceneIds.Contains(x.SceneId))
                        .Select(x => x.SceneId)
                        .Distinct()
                        .Count();

                    scenesPercent = (int)Math.Round((double)scenesCompleted * 100 / scenesTotal);

                    if (scenesPercent > 100)
                    {
                        scenesPercent = 100;
                    }
                }
            }

            var userCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.CourseId == courseId);

            bool isCompletedState = userCourse != null && userCourse.IsCompleted;

            string status;

            if (!isCompletedState && completedLessons == 0 && userCourse == null && scenesCompleted == 0)
            {
                status = "NotStarted";
            }
            else if (isCompletedState || (completedLessons >= totalLessons && (!scenesIncluded || scenesCompleted >= scenesTotal)))
            {
                status = "Completed";
                percent = 100;

                if (scenesIncluded)
                {
                    scenesPercent = 100;
                }

                EnsureCompletedState(userId, courseId, userCourse);

                userCourse = _dbContext.UserCourses.FirstOrDefault(x => x.UserId == userId && x.CourseId == courseId);
                isCompletedState = userCourse != null && userCourse.IsCompleted;
            }
            else
            {
                status = "InProgress";
            }

            int? nextLessonId = remaining.Count > 0 ? remaining[0] : null;

            return new CourseCompletionResponse
            {
                CourseId = courseId,
                Status = status,

                IsCompleted = isCompletedState,
                CompletedAt = userCourse?.CompletedAt,

                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                CompletionPercent = percent,
                NextLessonId = nextLessonId,
                RemainingLessonIds = remaining,

                ScenesIncluded = scenesIncluded,
                ScenesTotal = scenesTotal,
                ScenesCompleted = scenesCompleted,
                ScenesCompletionPercent = scenesPercent
            };
        }

        private void EnsureCompletedState(int userId, int courseId, UserCourse? userCourse)
        {
            if (userCourse != null && userCourse.IsCompleted)
            {
                return;
            }

            var now = _dateTimeProvider.UtcNow;

            if (userCourse == null)
            {
                userCourse = new UserCourse
                {
                    UserId = userId,
                    CourseId = courseId,
                    IsActive = false,
                    IsCompleted = true,
                    CompletedAt = now,
                    LastLessonId = null,
                    StartedAt = now,
                    LastOpenedAt = now
                };

                _dbContext.UserCourses.Add(userCourse);
                _dbContext.SaveChanges();
                return;
            }

            userCourse.IsCompleted = true;
            userCourse.CompletedAt = now;

            _dbContext.SaveChanges();
        }
    }
}
