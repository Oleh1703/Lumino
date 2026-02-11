using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class LearningPathService : ILearningPathService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly LearningSettings _learningSettings;

        public LearningPathService(
            LuminoDbContext dbContext,
            Microsoft.Extensions.Options.IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _learningSettings = learningSettings.Value;
        }

        public LearningPathResponse GetMyCoursePath(int userId, int courseId)
        {
            var course = _dbContext.Courses.FirstOrDefault(x => x.Id == courseId && x.IsPublished);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            var bestResults = _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .GroupBy(x => x.LessonId)
                .Select(g => new
                {
                    LessonId = g.Key,
                    BestScore = g.Max(x => x.Score),
                    TotalQuestions = g.Max(x => x.TotalQuestions)
                })
                .ToDictionary(x => x.LessonId, x => x);

            var orderedLessons =
                (from t in _dbContext.Topics
                 join l in _dbContext.Lessons on t.Id equals l.TopicId
                 where t.CourseId == course.Id
                 orderby t.Order, l.Order
                 select new
                 {
                     TopicId = t.Id,
                     TopicTitle = t.Title,
                     TopicOrder = t.Order,
                     LessonId = l.Id,
                     LessonTitle = l.Title,
                     LessonOrder = l.Order
                 })
                .ToList();

            var lessonStates = new Dictionary<int, (bool IsUnlocked, bool IsPassed, int? BestScore, int? TotalQuestions, int? BestPercent)>();

            bool previousPassed = true;

            for (int i = 0; i < orderedLessons.Count; i++)
            {
                var lessonId = orderedLessons[i].LessonId;

                bool isUnlocked = i == 0 || previousPassed;

                int? bestScore = null;
                int? totalQuestions = null;
                int? bestPercent = null;

                bool isPassed = false;

                if (bestResults.TryGetValue(lessonId, out var best))
                {
                    bestScore = best.BestScore;
                    totalQuestions = best.TotalQuestions;

                    if (totalQuestions.HasValue && totalQuestions.Value > 0 && bestScore.HasValue)
                    {
                        bestPercent = (int)Math.Round(bestScore.Value * 100.0 / totalQuestions.Value);
                        isPassed = bestScore.Value * 100 >= totalQuestions.Value * passingScorePercent;
                    }
                }

                lessonStates[lessonId] = (isUnlocked, isPassed, bestScore, totalQuestions, bestPercent);

                previousPassed = isPassed;
            }

            var topics = orderedLessons
                .GroupBy(x => new { x.TopicId, x.TopicTitle, x.TopicOrder })
                .OrderBy(x => x.Key.TopicOrder)
                .Select(g => new LearningPathTopicResponse
                {
                    Id = g.Key.TopicId,
                    Title = g.Key.TopicTitle,
                    Order = g.Key.TopicOrder,
                    Lessons = g
                        .OrderBy(x => x.LessonOrder)
                        .Select(x =>
                        {
                            var st = lessonStates[x.LessonId];

                            return new LearningPathLessonResponse
                            {
                                Id = x.LessonId,
                                Title = x.LessonTitle,
                                Order = x.LessonOrder,
                                IsUnlocked = st.IsUnlocked,
                                IsPassed = st.IsPassed,
                                BestScore = st.BestScore,
                                TotalQuestions = st.TotalQuestions,
                                BestPercent = st.BestPercent
                            };
                        })
                        .ToList()
                })
                .ToList();

            return new LearningPathResponse
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Topics = topics
            };
        }
    }
}
