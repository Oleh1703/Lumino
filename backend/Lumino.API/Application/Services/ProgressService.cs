using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;

namespace Lumino.Api.Application.Services
{
    public class ProgressService : IProgressService
    {
        private readonly LuminoDbContext _dbContext;

        public ProgressService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public UserProgressResponse GetMyProgress(int userId)
        {
            var progress = _dbContext.UserProgresses
                .FirstOrDefault(x => x.UserId == userId);

            if (progress == null)
            {
                return new UserProgressResponse
                {
                    CompletedLessons = 0,
                    TotalScore = 0,
                    LastUpdatedAt = DateTime.UtcNow
                };
            }

            return new UserProgressResponse
            {
                CompletedLessons = progress.CompletedLessons,
                TotalScore = progress.TotalScore,
                LastUpdatedAt = progress.LastUpdatedAt
            };
        }
    }
}
