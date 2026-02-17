using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using System.Text.Json;

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
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
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
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            int order = NormalizeOrder(request.Order);

            ValidateExercise(request.Type, request.Question, request.Data, request.CorrectAnswer);

            ValidateOrderUnique(request.LessonId, 0, order);

            var exercise = new Exercise
            {
                LessonId = request.LessonId,
                Type = Enum.Parse<ExerciseType>(request.Type),
                Question = request.Question,
                Data = request.Data,
                CorrectAnswer = request.CorrectAnswer,
                Order = order
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
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var exercise = _dbContext.Exercises.FirstOrDefault(x => x.Id == id);

            if (exercise == null)
            {
                throw new KeyNotFoundException("Exercise not found");
            }

            int order = NormalizeOrder(request.Order);

            ValidateExercise(request.Type, request.Question, request.Data, request.CorrectAnswer);

            ValidateOrderUnique(exercise.LessonId, exercise.Id, order);

            exercise.Type = Enum.Parse<ExerciseType>(request.Type);
            exercise.Question = request.Question;
            exercise.Data = request.Data;
            exercise.CorrectAnswer = request.CorrectAnswer;
            exercise.Order = order;

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var exercise = _dbContext.Exercises.FirstOrDefault(x => x.Id == id);

            if (exercise == null)
            {
                throw new KeyNotFoundException("Exercise not found");
            }

            _dbContext.Exercises.Remove(exercise);
            _dbContext.SaveChanges();
        }

        private int NormalizeOrder(int order)
        {
            if (order < 0)
            {
                return 0;
            }

            return order;
        }

        private void ValidateOrderUnique(int lessonId, int exerciseId, int order)
        {
            if (order <= 0)
            {
                return;
            }

            bool exists = _dbContext.Exercises.Any(x =>
                x.LessonId == lessonId &&
                x.Order == order &&
                x.Id != exerciseId);

            if (exists)
            {
                throw new ArgumentException("Exercise with this Order already exists in this lesson");
            }
        }

        private static void ValidateExercise(string type, string question, string data, string correctAnswer)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("Type is required");
            }

            if (!Enum.TryParse<ExerciseType>(type, out var exerciseType))
            {
                throw new ArgumentException("Type is invalid");
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Question is required");
            }

            if (string.IsNullOrWhiteSpace(data))
            {
                throw new ArgumentException("Data is required");
            }

            if (correctAnswer == null)
            {
                throw new ArgumentException("CorrectAnswer is required");
            }

            if (exerciseType == ExerciseType.MultipleChoice)
            {
                ValidateMultipleChoiceData(data, correctAnswer);
                return;
            }

            if (exerciseType == ExerciseType.Input)
            {
                ValidateInputData(data, correctAnswer);
                return;
            }

            if (exerciseType == ExerciseType.Match)
            {
                ValidateMatchData(data, correctAnswer);
                return;
            }
        }

        private static void ValidateMultipleChoiceData(string data, string correctAnswer)
        {
            var options = ParseStringArray(data);

            if (options.Count < 2)
            {
                throw new ArgumentException("MultipleChoice requires at least 2 options");
            }

            if (options.Any(x => string.IsNullOrWhiteSpace(x)))
            {
                throw new ArgumentException("MultipleChoice options are invalid");
            }

            if (string.IsNullOrWhiteSpace(correctAnswer))
            {
                throw new ArgumentException("CorrectAnswer is required");
            }

            bool contains = options.Any(x => string.Equals(x, correctAnswer, StringComparison.OrdinalIgnoreCase));

            if (!contains)
            {
                throw new ArgumentException("CorrectAnswer must be one of options");
            }
        }

        private static void ValidateInputData(string data, string correctAnswer)
        {
            if (string.IsNullOrWhiteSpace(correctAnswer))
            {
                throw new ArgumentException("CorrectAnswer is required");
            }

            try
            {
                using var doc = JsonDocument.Parse(data);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new ArgumentException("Input Data must be a JSON object");
                }
            }
            catch (JsonException)
            {
                throw new ArgumentException("Data is invalid JSON");
            }
        }

        private static void ValidateMatchData(string data, string correctAnswer)
        {
            if (!string.IsNullOrWhiteSpace(correctAnswer) && correctAnswer.Trim() != "{}")
            {
                throw new ArgumentException("CorrectAnswer must be empty for Match");
            }

            try
            {
                using var doc = JsonDocument.Parse(data);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw new ArgumentException("Match Data must be a JSON array");
                }

                var leftSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var rightSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                int count = 0;

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        throw new ArgumentException("Match Data items must be objects");
                    }

                    if (!item.TryGetProperty("left", out var leftProp) || leftProp.ValueKind != JsonValueKind.String)
                    {
                        throw new ArgumentException("Match Data item.left is required");
                    }

                    if (!item.TryGetProperty("right", out var rightProp) || rightProp.ValueKind != JsonValueKind.String)
                    {
                        throw new ArgumentException("Match Data item.right is required");
                    }

                    var left = leftProp.GetString() ?? string.Empty;
                    var right = rightProp.GetString() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                    {
                        throw new ArgumentException("Match Data values are invalid");
                    }

                    if (!leftSet.Add(left))
                    {
                        throw new ArgumentException("Match Data has duplicate left values");
                    }

                    if (!rightSet.Add(right))
                    {
                        throw new ArgumentException("Match Data has duplicate right values");
                    }

                    count++;
                }

                if (count < 2)
                {
                    throw new ArgumentException("Match requires at least 2 pairs");
                }
            }
            catch (JsonException)
            {
                throw new ArgumentException("Data is invalid JSON");
            }
        }

        private static List<string> ParseStringArray(string data)
        {
            try
            {
                using var doc = JsonDocument.Parse(data);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw new ArgumentException("Data is invalid JSON");
                }

                var list = new List<string>();

                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.String)
                    {
                        throw new ArgumentException("Data is invalid JSON");
                    }

                    list.Add(el.GetString() ?? string.Empty);
                }

                return list;
            }
            catch (JsonException)
            {
                throw new ArgumentException("Data is invalid JSON");
            }
        }
    }
}
