using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;

namespace Lumino.Api.Application.Services
{
    public class CourseCompletionService : ICourseCompletionService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly LearningSettings _learningSettings;

        public CourseCompletionService(
            LuminoDbContext dbContext,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
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

            var userCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.CourseId == courseId);

            string status;

            if (completedLessons == 0 && userCourse == null)
            {
                status = "NotStarted";
            }
            else if (completedLessons >= totalLessons)
            {
                status = "Completed";
                percent = 100;
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
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                CompletionPercent = percent,
                NextLessonId = nextLessonId,
                RemainingLessonIds = remaining,
                ScenesIncluded = false
            };
        }
    }
}
