using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Microsoft.Extensions.Configuration;

namespace Lumino.Api.Application.Services
{
    public class RefreshTokenCleanupService : IRefreshTokenCleanupService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public RefreshTokenCleanupService(LuminoDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public int Cleanup()
        {
            var now = DateTime.UtcNow;

            var refreshSection = _configuration.GetSection("RefreshToken");

            var keepRevokedDaysText = refreshSection["KeepRevokedDays"];

            if (!int.TryParse(keepRevokedDaysText, out var keepRevokedDays))
            {
                keepRevokedDays = 30;
            }

            if (keepRevokedDays < 1)
            {
                keepRevokedDays = 1;
            }

            var keepUntil = now.AddDays(-keepRevokedDays);

            var tokensToDelete = _dbContext.RefreshTokens
                .Where(x =>
                    x.ExpiresAt <= now
                    || (x.RevokedAt != null && x.RevokedAt <= keepUntil)
                )
                .ToList();

            if (tokensToDelete.Count == 0)
            {
                return 0;
            }

            _dbContext.RefreshTokens.RemoveRange(tokensToDelete);
            _dbContext.SaveChanges();

            return tokensToDelete.Count;
        }
    }
}
