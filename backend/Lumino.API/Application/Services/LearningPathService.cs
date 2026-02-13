using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
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
                .Select(g => new BestLessonResult
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
                 select new OrderedLessonInfo
                 {
                     TopicId = t.Id,
                     TopicTitle = t.Title,
                     TopicOrder = t.Order,
                     LessonId = l.Id,
                     LessonTitle = l.Title,
                     LessonOrder = l.Order
                 })
                .ToList();

            EnsureUserLessonProgressIsInSync(userId, orderedLessons, bestResults, passingScorePercent);

            var progressDict = _dbContext.UserLessonProgresses
                .Where(x => x.UserId == userId)
                .ToDictionary(x => x.LessonId, x => x);

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
                            progressDict.TryGetValue(x.LessonId, out var p);

                            int? totalQuestions = null;
                            int? bestPercent = null;

                            if (bestResults.TryGetValue(x.LessonId, out var best))
                            {
                                totalQuestions = best.TotalQuestions;

                                if (totalQuestions.HasValue && totalQuestions.Value > 0)
                                {
                                    var scoreForPercent = p != null ? p.BestScore : best.BestScore;
                                    bestPercent = (int)Math.Round(scoreForPercent * 100.0 / totalQuestions.Value);
                                }
                            }

                            return new LearningPathLessonResponse
                            {
                                Id = x.LessonId,
                                Title = x.LessonTitle,
                                Order = x.LessonOrder,
                                IsUnlocked = p != null && p.IsUnlocked,
                                IsPassed = p != null && p.IsCompleted,
                                BestScore = p != null ? p.BestScore : null,
                                TotalQuestions = totalQuestions,
                                BestPercent = bestPercent
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

        private void EnsureUserLessonProgressIsInSync(
            int userId,
            List<OrderedLessonInfo> orderedLessons,
            Dictionary<int, BestLessonResult> bestResults,
            int passingScorePercent)
        {
            if (orderedLessons == null || orderedLessons.Count == 0)
            {
                return;
            }

            var lessonIds = orderedLessons.Select(x => x.LessonId).ToList();

            var existing = _dbContext.UserLessonProgresses
                .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonId))
                .ToDictionary(x => x.LessonId, x => x);

            bool needSave = false;
            bool previousCompleted = false;

            for (int i = 0; i < orderedLessons.Count; i++)
            {
                int lessonId = orderedLessons[i].LessonId;

                int bestScore = 0;
                int? totalQuestions = null;
                bool passedFromResults = false;

                if (bestResults.TryGetValue(lessonId, out var best))
                {
                    bestScore = best.BestScore;
                    totalQuestions = best.TotalQuestions;

                    if (totalQuestions.HasValue && totalQuestions.Value > 0)
                    {
                        passedFromResults = bestScore * 100 >= totalQuestions.Value * passingScorePercent;
                    }
                }

                bool shouldBeUnlocked = i == 0 || previousCompleted;

                if (!existing.TryGetValue(lessonId, out var p))
                {
                    p = new UserLessonProgress
                    {
                        UserId = userId,
                        LessonId = lessonId,
                        IsUnlocked = shouldBeUnlocked,
                        IsCompleted = passedFromResults,
                        BestScore = bestScore,
                        LastAttemptAt = null
                    };

                    _dbContext.UserLessonProgresses.Add(p);
                    existing[lessonId] = p;
                    needSave = true;
                }
                else
                {
                    if (shouldBeUnlocked && !p.IsUnlocked)
                    {
                        p.IsUnlocked = true;
                        needSave = true;
                    }

                    if (passedFromResults && !p.IsCompleted)
                    {
                        p.IsCompleted = true;
                        needSave = true;
                    }

                    if (bestScore > p.BestScore)
                    {
                        p.BestScore = bestScore;
                        needSave = true;
                    }
                }

                previousCompleted = p.IsCompleted;
            }

            if (needSave)
            {
                _dbContext.SaveChanges();
            }
        }

        private class OrderedLessonInfo
        {
            public int TopicId { get; set; }
            public string TopicTitle { get; set; } = "";
            public int TopicOrder { get; set; }
            public int LessonId { get; set; }
            public string LessonTitle { get; set; } = "";
            public int LessonOrder { get; set; }
        }

        private class BestLessonResult
        {
            public int LessonId { get; set; }
            public int BestScore { get; set; }
            public int TotalQuestions { get; set; }
        }
    }
}
