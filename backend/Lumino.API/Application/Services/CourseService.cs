using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;

namespace Lumino.Api.Application.Services
{
    public class CourseService : ICourseService
    {
        private readonly LuminoDbContext _dbContext;

        public CourseService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<CourseResponse> GetPublishedCourses()
        {
            return _dbContext.Courses
                .Where(x => x.IsPublished)
                .Select(x => new CourseResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description
                })
                .ToList();
        }
    }
}
