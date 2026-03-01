using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;

namespace Lumino.Api.Application.Services
{
    public class UserService : IUserService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IUpdateProfileRequestValidator _updateProfileRequestValidator;
        private readonly LearningSettings _learningSettings;

        public UserService(LuminoDbContext dbContext, IUpdateProfileRequestValidator updateProfileRequestValidator, IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _updateProfileRequestValidator = updateProfileRequestValidator;
            _learningSettings = learningSettings.Value;
        }

        public UserProfileResponse GetCurrentUser(int userId)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }


            var (currentStreak, bestStreak) = GetStreakValues(userId);
            return new UserProfileResponse
            {
                Id = user.Id,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                NativeLanguageCode = user.NativeLanguageCode,
                TargetLanguageCode = user.TargetLanguageCode,
                Hearts = user.Hearts,
                Crystals = user.Crystals,
                HeartsMax = HeartsEconomyCalculator.GetHeartsMax(_learningSettings),
                HeartRegenMinutes = HeartsEconomyCalculator.GetHeartRegenMinutes(_learningSettings),
                CrystalCostPerHeart = HeartsEconomyCalculator.GetCrystalCostPerHeart(_learningSettings),
                NextHeartAtUtc = HeartsEconomyCalculator.GetNextHeartAtUtc(user.Hearts, user.HeartsUpdatedAtUtc, _learningSettings),
                NextHeartInSeconds = HeartsEconomyCalculator.GetNextHeartInSeconds(user.Hearts, user.HeartsUpdatedAtUtc, _learningSettings),
                Theme = string.IsNullOrWhiteSpace(user.Theme) ? "light" : user.Theme,
                CurrentStreakDays = currentStreak,
                BestStreakDays = bestStreak
            };
        }

        public UserProfileResponse UpdateProfile(int userId, UpdateProfileRequest request)
        {
            _updateProfileRequestValidator.Validate(request);

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var username = request.Username.Trim();

                var exists = _dbContext.Users.Any(x => x.Username == username && x.Id != userId);

                if (exists)
                {
                    throw new ArgumentException("Username already exists");
                }

                user.Username = username;
            }

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Theme))
            {
                user.Theme = request.Theme.Trim().ToLowerInvariant();
            }

            _dbContext.SaveChanges();

            return GetCurrentUser(userId);
        }

        private (int current, int best) GetStreakValues(int userId)
        {
            var todayUtc = DateTime.UtcNow.Date;

            var streak = _dbContext.UserStreaks.FirstOrDefault(x => x.UserId == userId);

            if (streak == null)
            {
                return (0, 0);
            }

            var lastDate = streak.LastActivityDateUtc.Date;

            if (lastDate < todayUtc.AddDays(-1) && streak.CurrentStreak != 0)
            {
                streak.CurrentStreak = 0;
                _dbContext.SaveChanges();
            }

            return (streak.CurrentStreak, streak.BestStreak);
        }

    }
}
