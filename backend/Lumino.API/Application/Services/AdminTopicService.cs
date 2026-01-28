using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;

namespace Lumino.Api.Application.Services
{
    public class AdminTopicService : IAdminTopicService
    {
        private readonly LuminoDbContext _dbContext;

        public AdminTopicService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AdminTopicResponse> GetByCourse(int courseId)
        {
            return _dbContext.Topics
                .Where(x => x.CourseId == courseId)
                .OrderBy(x => x.Order)
                .Select(x => new AdminTopicResponse
                {
                    Id = x.Id,
                    CourseId = x.CourseId,
                    Title = x.Title,
                    Order = x.Order
                })
                .ToList();
        }

        public AdminTopicResponse Create(CreateTopicRequest request)
        {
            var topic = new Topic
            {
                CourseId = request.CourseId,
                Title = request.Title,
                Order = request.Order
            };

            _dbContext.Topics.Add(topic);
            _dbContext.SaveChanges();

            return new AdminTopicResponse
            {
                Id = topic.Id,
                CourseId = topic.CourseId,
                Title = topic.Title,
                Order = topic.Order
            };
        }

        public void Update(int id, UpdateTopicRequest request)
        {
            var topic = _dbContext.Topics.First(x => x.Id == id);

            topic.Title = request.Title;
            topic.Order = request.Order;

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var topic = _dbContext.Topics.First(x => x.Id == id);
            _dbContext.Topics.Remove(topic);
            _dbContext.SaveChanges();
        }
    }
}
