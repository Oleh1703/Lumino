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
                .OrderBy(x => x.Order)
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
                throw new ForbiddenAccessException("Lesson is locked");
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
    }
}
