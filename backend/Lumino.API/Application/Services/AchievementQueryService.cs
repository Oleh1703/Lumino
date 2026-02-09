using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using System.Collections.Generic;

namespace Lumino.Api.Application.Services
{
    public class AchievementQueryService : IAchievementQueryService
    {
        private readonly LuminoDbContext _dbContext;

        public AchievementQueryService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AchievementResponse> GetUserAchievements(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("UserId is invalid");
            }

            var result = _dbContext.UserAchievements
                .Where(x => x.UserId == userId)
                .Join(
                    _dbContext.Achievements,
                    ua => ua.AchievementId,
                    a => a.Id,
                    (ua, a) => new AchievementResponse
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Description = a.Description,
                        EarnedAt = ua.EarnedAt
                    }
                )
                .ToList();

            return result;
        }
    }
}
