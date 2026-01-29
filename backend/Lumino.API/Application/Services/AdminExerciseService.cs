using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;

namespace Lumino.Api.Application.Services
{
    public class AdminExerciseService : IAdminExerciseService
    {
        private readonly LuminoDbContext _dbContext;

        public AdminExerciseService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AdminExerciseResponse> GetByLesson(int lessonId)
        {
            return _dbContext.Exercises
                .Where(x => x.LessonId == lessonId)
                .OrderBy(x => x.Order)
                .Select(x => new AdminExerciseResponse
                {
                    Id = x.Id,
                    LessonId = x.LessonId,
                    Type = x.Type.ToString(),
                    Question = x.Question,
                    Data = x.Data,
                    CorrectAnswer = x.CorrectAnswer,
                    Order = x.Order
                })
                .ToList();
        }

        public AdminExerciseResponse Create(CreateExerciseRequest request)
        {
            var exercise = new Exercise
            {
                LessonId = request.LessonId,
                Type = Enum.Parse<ExerciseType>(request.Type),
                Question = request.Question,
                Data = request.Data,
                CorrectAnswer = request.CorrectAnswer,
                Order = request.Order
            };

            _dbContext.Exercises.Add(exercise);
            _dbContext.SaveChanges();

            return new AdminExerciseResponse
            {
                Id = exercise.Id,
                LessonId = exercise.LessonId,
                Type = exercise.Type.ToString(),
                Question = exercise.Question,
                Data = exercise.Data,
                CorrectAnswer = exercise.CorrectAnswer,
                Order = exercise.Order
            };
        }

        public void Update(int id, UpdateExerciseRequest request)
        {
            var exercise = _dbContext.Exercises.First(x => x.Id == id);

            exercise.Type = Enum.Parse<ExerciseType>(request.Type);
            exercise.Question = request.Question;
            exercise.Data = request.Data;
            exercise.CorrectAnswer = request.CorrectAnswer;
            exercise.Order = request.Order;

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var exercise = _dbContext.Exercises.First(x => x.Id == id);
            _dbContext.Exercises.Remove(exercise);
            _dbContext.SaveChanges();
        }
    }
}
