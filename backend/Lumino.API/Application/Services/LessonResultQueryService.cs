using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;

namespace Lumino.Api.Application.Services
{
    public class LessonResultQueryService : ILessonResultQueryService
    {
        private readonly LuminoDbContext _dbContext;

        public LessonResultQueryService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
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
    }
}
