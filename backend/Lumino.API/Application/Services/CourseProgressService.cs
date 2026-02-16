using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;

namespace Lumino.Api.Application.Services
{
    public class CourseProgressService : ICourseProgressService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly LearningSettings _learningSettings;

        public CourseProgressService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _learningSettings = learningSettings.Value;
        }

        public ActiveCourseResponse StartCourse(int userId, int courseId)
        {
            var course = _dbContext.Courses.FirstOrDefault(x => x.Id == courseId && x.IsPublished);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            var orderedLessons = GetOrderedLessons(courseId);

            if (orderedLessons.Count == 0)
            {
                throw new KeyNotFoundException("Course has no lessons");
            }

            var firstLessonId = orderedLessons[0].LessonId;

            var now = _dateTimeProvider.UtcNow;

            var active = _dbContext.UserCourses
                .Where(x => x.UserId == userId && x.IsActive && x.CourseId != courseId)
                .ToList();

            foreach (var item in active)
            {
                item.IsActive = false;
                item.LastOpenedAt = now;
            }

            var userCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.CourseId == courseId);

            if (userCourse == null)
            {
                userCourse = new UserCourse
                {
                    UserId = userId,
                    CourseId = courseId,
                    IsActive = true,

                    IsCompleted = false,
                    CompletedAt = null,

                    StartedAt = now,
                    LastLessonId = firstLessonId,
                    LastOpenedAt = now
                };

                _dbContext.UserCourses.Add(userCourse);
            }
            else
            {
                userCourse.IsActive = true;

                if (userCourse.StartedAt == default)
                {
                    userCourse.StartedAt = now;
                }

                userCourse.LastOpenedAt = now;
            }

            // створюємо прогрес для всіх уроків курсу і робимо 1-й unlocked
            EnsureUserLessonProgressForCourse(userId, orderedLessons, now);

            // LastLessonId має вказувати на unlocked і непройдений урок (або на перший)
            EnsureLastLessonIdIsValid(userId, userCourse, orderedLessons, firstLessonId);

            _dbContext.SaveChanges();

            return new ActiveCourseResponse
            {
                CourseId = userCourse.CourseId,
                StartedAt = userCourse.StartedAt,
                LastLessonId = userCourse.LastLessonId,
                LastOpenedAt = userCourse.LastOpenedAt
            };
        }

        public ActiveCourseResponse? GetMyActiveCourse(int userId)
        {
            var userCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.IsActive);

            if (userCourse == null)
            {
                return null;
            }

            return new ActiveCourseResponse
            {
                CourseId = userCourse.CourseId,
                StartedAt = userCourse.StartedAt,
                LastLessonId = userCourse.LastLessonId,
                LastOpenedAt = userCourse.LastOpenedAt
            };
        }

        public List<UserLessonProgressResponse> GetMyLessonProgressByCourse(int userId, int courseId)
        {
            var lessons = (
                from t in _dbContext.Topics
                join l in _dbContext.Lessons on t.Id equals l.TopicId
                where t.CourseId == courseId
                orderby (t.Order <= 0 ? int.MaxValue : t.Order), t.Id, (l.Order <= 0 ? int.MaxValue : l.Order), l.Id
                select l.Id
            ).ToList();

            var dict = _dbContext.UserLessonProgresses
                .Where(x => x.UserId == userId)
                .ToDictionary(x => x.LessonId, x => x);

            var result = new List<UserLessonProgressResponse>();

            foreach (var lessonId in lessons)
            {
                if (dict.TryGetValue(lessonId, out var p))
                {
                    result.Add(new UserLessonProgressResponse
                    {
                        LessonId = lessonId,
                        IsUnlocked = p.IsUnlocked,
                        IsCompleted = p.IsCompleted,
                        BestScore = p.BestScore,
                        LastAttemptAt = p.LastAttemptAt
                    });

                    continue;
                }

                result.Add(new UserLessonProgressResponse
                {
                    LessonId = lessonId,
                    IsUnlocked = false,
                    IsCompleted = false,
                    BestScore = 0,
                    LastAttemptAt = null
                });
            }

            return result;
        }

        private List<OrderedLessonInfo> GetOrderedLessons(int courseId)
        {
            return (
                from t in _dbContext.Topics
                join l in _dbContext.Lessons on t.Id equals l.TopicId
                where t.CourseId == courseId
                orderby (t.Order <= 0 ? int.MaxValue : t.Order), t.Id, (l.Order <= 0 ? int.MaxValue : l.Order), l.Id
                select new OrderedLessonInfo
                {
                    LessonId = l.Id
                }
            ).ToList();
        }

        private void EnsureUserLessonProgressForCourse(int userId, List<OrderedLessonInfo> orderedLessons, DateTime now)
        {
            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            var lessonIds = orderedLessons.Select(x => x.LessonId).ToList();

            var passedLessonIds = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    lessonIds.Contains(x.LessonId) &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .Select(x => x.LessonId)
                .Distinct()
                .ToList();

            var bestScores = _dbContext.LessonResults
                .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonId))
                .GroupBy(x => x.LessonId)
                .Select(g => new
                {
                    LessonId = g.Key,
                    BestScore = g.Max(x => x.Score)
                })
                .ToDictionary(x => x.LessonId, x => x.BestScore);

            var existing = _dbContext.UserLessonProgresses
                .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonId))
                .ToDictionary(x => x.LessonId, x => x);

            bool needSave = false;
            bool previousCompleted = false;

            for (int i = 0; i < orderedLessons.Count; i++)
            {
                int lessonId = orderedLessons[i].LessonId;

                bool shouldBeUnlocked = i == 0 || previousCompleted;
                bool isPassed = passedLessonIds.Contains(lessonId);

                int bestScore = 0;

                if (bestScores.TryGetValue(lessonId, out var bs))
                {
                    bestScore = bs;
                }

                if (!existing.TryGetValue(lessonId, out var p))
                {
                    p = new UserLessonProgress
                    {
                        UserId = userId,
                        LessonId = lessonId,
                        IsUnlocked = shouldBeUnlocked,
                        IsCompleted = isPassed,
                        BestScore = bestScore,
                        LastAttemptAt = shouldBeUnlocked ? now : null
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

                    if (shouldBeUnlocked && p.LastAttemptAt == null)
                    {
                        p.LastAttemptAt = now;
                        needSave = true;
                    }

                    if (isPassed && !p.IsCompleted)
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

        private void EnsureLastLessonIdIsValid(
            int userId,
            UserCourse userCourse,
            List<OrderedLessonInfo> orderedLessons,
            int firstLessonId)
        {
            var lessonIds = orderedLessons.Select(x => x.LessonId).ToList();

            var progressDict = _dbContext.UserLessonProgresses
                .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonId))
                .ToDictionary(x => x.LessonId, x => x);

            int targetLessonId = firstLessonId;

            foreach (var item in orderedLessons)
            {
                if (progressDict.TryGetValue(item.LessonId, out var p))
                {
                    if (p.IsUnlocked && !p.IsCompleted)
                    {
                        targetLessonId = item.LessonId;
                        break;
                    }
                }
            }

            if (userCourse.LastLessonId == null)
            {
                userCourse.LastLessonId = targetLessonId;
                return;
            }

            var current = userCourse.LastLessonId.Value;

            if (!lessonIds.Contains(current))
            {
                userCourse.LastLessonId = targetLessonId;
                return;
            }

            if (progressDict.TryGetValue(current, out var cp))
            {
                if (!cp.IsUnlocked || cp.IsCompleted)
                {
                    userCourse.LastLessonId = targetLessonId;
                }
            }
            else
            {
                userCourse.LastLessonId = targetLessonId;
            }
        }

        private class OrderedLessonInfo
        {
            public int LessonId { get; set; }
        }
    }
}
