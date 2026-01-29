using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;

namespace Lumino.Api.Application.Services
{
    public class AdminLessonService : IAdminLessonService
    {
        private readonly LuminoDbContext _dbContext;

        public AdminLessonService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AdminLessonResponse> GetByTopic(int topicId)
        {
            return _dbContext.Lessons
                .Where(x => x.TopicId == topicId)
                .OrderBy(x => x.Order)
                .Select(x => new AdminLessonResponse
                {
                    Id = x.Id,
                    TopicId = x.TopicId,
                    Title = x.Title,
                    Theory = x.Theory,
                    Order = x.Order
                })
                .ToList();
        }

        public AdminLessonResponse Create(CreateLessonRequest request)
        {
            var lesson = new Lesson
            {
                TopicId = request.TopicId,
                Title = request.Title,
                Theory = request.Theory,
                Order = request.Order
            };

            _dbContext.Lessons.Add(lesson);
            _dbContext.SaveChanges();

            return new AdminLessonResponse
            {
                Id = lesson.Id,
                TopicId = lesson.TopicId,
                Title = lesson.Title,
                Theory = lesson.Theory,
                Order = lesson.Order
            };
        }

        public void Update(int id, UpdateLessonRequest request)
        {
            var lesson = _dbContext.Lessons.First(x => x.Id == id);

            lesson.Title = request.Title;
            lesson.Theory = request.Theory;
            lesson.Order = request.Order;

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var lesson = _dbContext.Lessons.First(x => x.Id == id);
            _dbContext.Lessons.Remove(lesson);
            _dbContext.SaveChanges();
        }
    }
}
