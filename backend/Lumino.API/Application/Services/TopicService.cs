using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;

namespace Lumino.Api.Application.Services
{
    public class TopicService : ITopicService
    {
        private readonly LuminoDbContext _dbContext;

        public TopicService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<TopicResponse> GetTopicsByCourse(int courseId)
        {
            var course = _dbContext.Courses.First(x => x.Id == courseId && x.IsPublished);

            return _dbContext.Topics
                .Where(x => x.CourseId == course.Id)
                .OrderBy(x => x.Order)
                .Select(x => new TopicResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Order = x.Order
                })
                .ToList();
        }
    }
}
