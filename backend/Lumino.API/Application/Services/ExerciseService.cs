using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class ExerciseService : IExerciseService
    {
        private readonly LuminoDbContext _dbContext;

        public ExerciseService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<ExerciseResponse> GetExercisesByLesson(int userId, int lessonId)
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
                .FirstOrDefault(x => x.UserId == userId && x.LessonId == lessonId);

            if (progress == null || !progress.IsUnlocked)
            {
                throw new ForbiddenAccessException("Lesson is locked");
            }

            return _dbContext.Exercises
                .Where(x => x.LessonId == lesson.Id)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
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
