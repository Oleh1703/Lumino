using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;

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
                    Title = x.Title,
                    Theory = x.Theory,
                    Order = x.Order
                })
                .ToList();
        }
    }
}
