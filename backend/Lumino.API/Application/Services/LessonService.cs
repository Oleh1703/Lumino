using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class LessonService : ILessonService
    {
        private readonly LuminoDbContext _dbContext;

        public LessonService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<LessonResponse> GetLessonsByTopic(int topicId)
        {
            var topic = _dbContext.Topics.FirstOrDefault(x => x.Id == topicId);

            if (topic == null)
            {
                throw new KeyNotFoundException("Topic not found");
            }

            var course = _dbContext.Courses.FirstOrDefault(x => x.Id == topic.CourseId && x.IsPublished);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            return _dbContext.Lessons
                .Where(x => x.TopicId == topic.Id)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Select(x => new LessonResponse
                {
                    Id = x.Id,
                    TopicId = x.TopicId,
                    Title = x.Title,
                    Theory = x.Theory,
                    Order = x.Order
                })
                .ToList();
        }

        public LessonResponse GetLessonById(int userId, int lessonId)
        {
            var lesson = _dbContext.Lessons.FirstOrDefault(x => x.Id == lessonId);

            if (lesson == null)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            var topic = _dbContext.Topics.FirstOrDefault(x => x.Id == lesson.TopicId);

            if (topic == null)
            {
                throw new KeyNotFoundException("Topic not found");
            }

            var course = _dbContext.Courses.FirstOrDefault(x => x.Id == topic.CourseId && x.IsPublished);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            var progress = _dbContext.UserLessonProgresses
                .FirstOrDefault(x => x.UserId == userId && x.LessonId == lesson.Id);

            if (progress == null || !progress.IsUnlocked)
            {
                throw new ForbiddenAccessException(GetLessonLockedMessage(course.Id, lessonId));
            }

            return new LessonResponse
            {
                Id = lesson.Id,
                TopicId = lesson.TopicId,
                Title = lesson.Title,
                Theory = lesson.Theory,
                Order = lesson.Order
            };
        }
        private string GetLessonLockedMessage(int courseId, int lessonId)
        {
            var lessonIds = _dbContext.Lessons
                .Join(
                    _dbContext.Topics.Where(t => t.CourseId == courseId),
                    l => l.TopicId,
                    t => t.Id,
                    (l, t) => new
                    {
                        LessonId = l.Id,
                        TopicId = t.Id,
                        TopicOrder = t.Order,
                        LessonOrder = l.Order
                    })
                .OrderBy(x => x.TopicOrder <= 0 ? int.MaxValue : x.TopicOrder)
                .ThenBy(x => x.TopicId)
                .ThenBy(x => x.LessonOrder <= 0 ? int.MaxValue : x.LessonOrder)
                .ThenBy(x => x.LessonId)
                .Select(x => x.LessonId)
                .ToList();

            var index = lessonIds.IndexOf(lessonId);

            if (index <= 0)
            {
                return "Lesson is locked";
            }

            var requiredLessonId = lessonIds[index - 1];

            return $"Lesson is locked. Complete lesson {requiredLessonId} to unlock.";
        }
    }
}
