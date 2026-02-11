using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class CourseProgressService : ICourseProgressService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CourseProgressService(LuminoDbContext dbContext, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
        }

        public ActiveCourseResponse StartCourse(int userId, int courseId)
        {
            var course = _dbContext.Courses.FirstOrDefault(x => x.Id == courseId && x.IsPublished);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            var firstLessonId = GetFirstLessonId(courseId);

            if (firstLessonId == null)
            {
                throw new KeyNotFoundException("Course has no lessons");
            }

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

                if (userCourse.LastLessonId == null)
                {
                    userCourse.LastLessonId = firstLessonId;
                }

                userCourse.LastOpenedAt = now;
            }

            EnsureLessonUnlocked(userId, firstLessonId.Value, now);

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
                orderby t.Order, l.Order
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

        private int? GetFirstLessonId(int courseId)
        {
            return (
                from t in _dbContext.Topics
                join l in _dbContext.Lessons on t.Id equals l.TopicId
                where t.CourseId == courseId
                orderby t.Order, l.Order
                select (int?)l.Id
            ).FirstOrDefault();
        }

        private void EnsureLessonUnlocked(int userId, int lessonId, DateTime now)
        {
            var progress = _dbContext.UserLessonProgresses
                .FirstOrDefault(x => x.UserId == userId && x.LessonId == lessonId);

            if (progress == null)
            {
                _dbContext.UserLessonProgresses.Add(new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = lessonId,
                    IsUnlocked = true,
                    IsCompleted = false,
                    BestScore = 0,
                    LastAttemptAt = now
                });

                return;
            }

            if (!progress.IsUnlocked)
            {
                progress.IsUnlocked = true;
            }

            if (progress.LastAttemptAt == null)
            {
                progress.LastAttemptAt = now;
            }
        }
    }
}
