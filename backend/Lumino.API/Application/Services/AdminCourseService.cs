using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;

namespace Lumino.Api.Application.Services
{
    public class AdminCourseService : IAdminCourseService
    {
        private readonly LuminoDbContext _dbContext;

        public AdminCourseService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AdminCourseResponse> GetAll()
        {
            return _dbContext.Courses
                .Select(x => new AdminCourseResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    IsPublished = x.IsPublished
                })
                .ToList();
        }

        public AdminCourseResponse Create(CreateCourseRequest request)
        {
            var course = new Course
            {
                Title = request.Title,
                Description = request.Description,
                IsPublished = request.IsPublished
            };

            _dbContext.Courses.Add(course);
            _dbContext.SaveChanges();

            return new AdminCourseResponse
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                IsPublished = course.IsPublished
            };
        }

        public void Update(int id, UpdateCourseRequest request)
        {
            var course = _dbContext.Courses.First(x => x.Id == id);

            course.Title = request.Title;
            course.Description = request.Description;
            course.IsPublished = request.IsPublished;

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var course = _dbContext.Courses.First(x => x.Id == id);
            _dbContext.Courses.Remove(course);
            _dbContext.SaveChanges();
        }
    }
}
