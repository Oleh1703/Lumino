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
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
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
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var order = NormalizeOrder(request.Order);

            ValidateUniqueLessonOrder(request.TopicId, order, ignoreLessonId: null);

            var lesson = new Lesson
            {
                TopicId = request.TopicId,
                Title = request.Title,
                Theory = request.Theory,
                Order = order
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
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var lesson = _dbContext.Lessons.FirstOrDefault(x => x.Id == id);

            if (lesson == null)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            var order = NormalizeOrder(request.Order);

            ValidateUniqueLessonOrder(lesson.TopicId, order, ignoreLessonId: lesson.Id);

            lesson.Title = request.Title;
            lesson.Theory = request.Theory;
            lesson.Order = order;

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var lesson = _dbContext.Lessons.FirstOrDefault(x => x.Id == id);

            if (lesson == null)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            _dbContext.Lessons.Remove(lesson);
            _dbContext.SaveChanges();
        }

        private int NormalizeOrder(int order)
        {
            return order < 0 ? 0 : order;
        }

        private void ValidateUniqueLessonOrder(int topicId, int order, int? ignoreLessonId)
        {
            if (order <= 0)
            {
                return;
            }

            var hasDuplicate = _dbContext.Lessons.Any(x =>
                x.TopicId == topicId &&
                x.Order == order &&
                (ignoreLessonId == null || x.Id != ignoreLessonId));

            if (hasDuplicate)
            {
                throw new ArgumentException("Order is already used in this topic");
            }
        }
    }
}
