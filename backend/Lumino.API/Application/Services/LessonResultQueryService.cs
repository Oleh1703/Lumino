using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Lumino.Api.Application.Services
{
    public class LessonResultQueryService : ILessonResultQueryService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly LearningSettings _learningSettings;

        public LessonResultQueryService(LuminoDbContext dbContext, IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _learningSettings = learningSettings.Value;
        }

        public List<LessonResultResponse> GetMyResults(int userId)
        {
            return _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CompletedAt)
                .Join(
                    _dbContext.Lessons,
                    r => r.LessonId,
                    l => l.Id,
                    (r, l) => new LessonResultResponse
                    {
                        LessonId = r.LessonId,
                        LessonTitle = l.Title,
                        Score = r.Score,
                        TotalQuestions = r.TotalQuestions,
                        CompletedAt = r.CompletedAt
                    }
                )
                .ToList();
        }

        public LessonResultDetailsResponse GetMyResultDetails(int userId, int resultId)
        {
            var result = _dbContext.LessonResults.FirstOrDefault(x => x.Id == resultId && x.UserId == userId);

            if (result == null)
            {
                throw new KeyNotFoundException("Lesson result not found");
            }

            var lessonTitle = _dbContext.Lessons
                .Where(x => x.Id == result.LessonId)
                .Select(x => x.Title)
                .FirstOrDefault();

            var details = ParseDetails(result.MistakesJson);

            var passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);
            var isPassed = LessonPassingRules.IsPassed(result.Score, result.TotalQuestions, passingScorePercent);

            return new LessonResultDetailsResponse
            {
                ResultId = result.Id,
                LessonId = result.LessonId,
                LessonTitle = lessonTitle ?? string.Empty,
                Score = result.Score,
                TotalQuestions = result.TotalQuestions,
                IsPassed = isPassed,
                CompletedAt = result.CompletedAt,
                MistakeExerciseIds = details.MistakeExerciseIds,
                Answers = details.Answers
            };
        }

        private static LessonResultDetailsJson ParseDetails(string? mistakesJson)
        {
            if (string.IsNullOrWhiteSpace(mistakesJson))
            {
                return new LessonResultDetailsJson();
            }

            try
            {
                using var doc = JsonDocument.Parse(mistakesJson);

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var ids = JsonSerializer.Deserialize<List<int>>(mistakesJson) ?? new List<int>();

                    return new LessonResultDetailsJson
                    {
                        MistakeExerciseIds = ids
                    };
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var model = JsonSerializer.Deserialize<LessonResultDetailsJson>(
                        mistakesJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return model ?? new LessonResultDetailsJson();
                }
            }
            catch
            {
                // ignore and return empty
            }

            return new LessonResultDetailsJson();
        }
    }
}
