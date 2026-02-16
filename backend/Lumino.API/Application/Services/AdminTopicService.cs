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
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
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
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

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
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var topic = _dbContext.Topics.FirstOrDefault(x => x.Id == id);

            if (topic == null)
            {
                throw new KeyNotFoundException("Topic not found");
            }

            topic.Title = request.Title;
            topic.Order = request.Order;

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var topic = _dbContext.Topics.FirstOrDefault(x => x.Id == id);

            if (topic == null)
            {
                throw new KeyNotFoundException("Topic not found");
            }

            _dbContext.Topics.Remove(topic);
            _dbContext.SaveChanges();
        }
    }
}
