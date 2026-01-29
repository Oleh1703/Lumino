using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;

namespace Lumino.Api.Application.Services
{
    public class ExerciseService : IExerciseService
    {
        private readonly LuminoDbContext _dbContext;

        public ExerciseService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<ExerciseResponse> GetExercisesByLesson(int lessonId)
        {
            var lesson = _dbContext.Lessons.First(x => x.Id == lessonId);
            var topic = _dbContext.Topics.First(x => x.Id == lesson.TopicId);
            _dbContext.Courses.First(x => x.Id == topic.CourseId && x.IsPublished);

            return _dbContext.Exercises
                .Where(x => x.LessonId == lesson.Id)
                .OrderBy(x => x.Order)
                .Select(x => new ExerciseResponse
                {
                    Id = x.Id,
                    Type = x.Type.ToString(),
                    Question = x.Question,
                    Data = x.Data,
                    Order = x.Order
                })
                .ToList();
        }
    }
}
