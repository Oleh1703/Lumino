using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;

namespace Lumino.Api.Application.Services
{
    public class UserEconomyService : IUserEconomyService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly LearningSettings _learningSettings;

        public UserEconomyService(LuminoDbContext dbContext, IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _learningSettings = learningSettings.Value;
        }



        public void RefreshHearts(int userId)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            RegenerateHeartsIfNeeded(user);
        }

        public void EnsureHasHeartsOrThrow(int userId)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            RegenerateHeartsIfNeeded(user);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var heartsMax = _learningSettings.HeartsMax <= 0 ? 5 : _learningSettings.HeartsMax;

            if (user.Hearts > heartsMax)
            {
                user.Hearts = heartsMax;
                _dbContext.SaveChanges();
            }

            if (user.Hearts <= 0)
            {
                throw new ForbiddenAccessException("No hearts. Restore hearts to continue.");
            }
        }

        public void ConsumeHeartsForMistakes(int userId, int mistakesCount)
        {
            if (mistakesCount <= 0)
            {
                return;
            }

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            RegenerateHeartsIfNeeded(user);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var costPerMistake = _learningSettings.HeartsCostPerMistake <= 0 ? 1 : _learningSettings.HeartsCostPerMistake;
            var totalCost = mistakesCount * costPerMistake;

            if (totalCost <= 0)
            {
                return;
            }

            var newHearts = user.Hearts - totalCost;

            if (newHearts < 0)
            {
                newHearts = 0;
            }

            if (newHearts == user.Hearts)
            {
                return;
            }

            user.Hearts = newHearts;
            user.HeartsUpdatedAtUtc = DateTime.UtcNow;
            _dbContext.SaveChanges();
        }

        public void AwardCrystalsForPassedLessonIfNeeded(int userId)
        {
            var amount = _learningSettings.CrystalsRewardPerPassedLesson;

            if (amount <= 0)
            {
                return;
            }

            AwardCrystals(userId, amount);
        }

        public void AwardCrystalsForCompletedSceneIfNeeded(int userId)
        {
            var amount = _learningSettings.CrystalsRewardPerCompletedScene;

            if (amount <= 0)
            {
                return;
            }

            AwardCrystals(userId, amount);
        }

        public void AwardHeartForPracticeIfPossible(int userId)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            RegenerateHeartsIfNeeded(user);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var heartsMax = _learningSettings.HeartsMax <= 0 ? 5 : _learningSettings.HeartsMax;

            if (user.Hearts >= heartsMax)
            {
                return;
            }

            user.Hearts++;
            user.HeartsUpdatedAtUtc = DateTime.UtcNow;
            _dbContext.SaveChanges();
        }

        public RestoreHeartsResponse RestoreHearts(int userId, RestoreHeartsRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (request.HeartsToRestore <= 0)
            {
                throw new ArgumentException("HeartsToRestore must be greater than 0");
            }

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            RegenerateHeartsIfNeeded(user);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var heartsMax = _learningSettings.HeartsMax <= 0 ? 5 : _learningSettings.HeartsMax;
            var costPerHeart = _learningSettings.CrystalCostPerHeart <= 0 ? 10 : _learningSettings.CrystalCostPerHeart;

            if (user.Hearts >= heartsMax)
            {
                return new RestoreHeartsResponse
                {
                    Hearts = user.Hearts,
                    Crystals = user.Crystals,
                    SpentCrystals = 0,
                    RestoredHearts = 0
                };
            }

            var requested = request.HeartsToRestore;
            var canRestore = heartsMax - user.Hearts;

            if (requested > canRestore)
            {
                requested = canRestore;
            }

            var neededCrystals = requested * costPerHeart;

            if (user.Crystals < neededCrystals)
            {
                throw new ForbiddenAccessException("Not enough crystals");
            }

            user.Crystals -= neededCrystals;
            user.Hearts += requested;
            user.HeartsUpdatedAtUtc = DateTime.UtcNow;

            _dbContext.SaveChanges();

            return new RestoreHeartsResponse
            {
                Hearts = user.Hearts,
                Crystals = user.Crystals,
                SpentCrystals = neededCrystals,
                RestoredHearts = requested
            };
        }



        private void RegenerateHeartsIfNeeded(Lumino.Api.Domain.Entities.User? user)
        {
            if (user == null)
            {
                return;
            }

            var heartsMax = _learningSettings.HeartsMax <= 0 ? 5 : _learningSettings.HeartsMax;

            if (user.Hearts > heartsMax)
            {
                user.Hearts = heartsMax;
                user.HeartsUpdatedAtUtc = DateTime.UtcNow;
                _dbContext.SaveChanges();
                return;
            }

            var regenMinutes = _learningSettings.HeartRegenMinutes <= 0 ? 30 : _learningSettings.HeartRegenMinutes;

            if (regenMinutes <= 0)
            {
                return;
            }

            // Якщо сердечка повні — фіксуємо "останній час", щоб після списання не було миттєвого відновлення.
            if (user.Hearts >= heartsMax)
            {
                if (user.HeartsUpdatedAtUtc == null || user.HeartsUpdatedAtUtc.Value < DateTime.UtcNow.AddMinutes(-regenMinutes))
                {
                    user.HeartsUpdatedAtUtc = DateTime.UtcNow;
                    _dbContext.SaveChanges();
                }

                return;
            }

            if (user.HeartsUpdatedAtUtc == null)
            {
                user.HeartsUpdatedAtUtc = DateTime.UtcNow;
                _dbContext.SaveChanges();
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var elapsed = nowUtc - user.HeartsUpdatedAtUtc.Value;

            if (elapsed.TotalMinutes < regenMinutes)
            {
                return;
            }

            var increments = (int)(elapsed.TotalMinutes / regenMinutes);

            if (increments <= 0)
            {
                return;
            }

            var newHearts = user.Hearts + increments;

            if (newHearts > heartsMax)
            {
                newHearts = heartsMax;
            }

            if (newHearts == user.Hearts)
            {
                user.HeartsUpdatedAtUtc = nowUtc;
                _dbContext.SaveChanges();
                return;
            }

            user.Hearts = newHearts;
            user.HeartsUpdatedAtUtc = user.HeartsUpdatedAtUtc.Value.AddMinutes(increments * regenMinutes);

            if (user.HeartsUpdatedAtUtc > nowUtc)
            {
                user.HeartsUpdatedAtUtc = nowUtc;
            }

            _dbContext.SaveChanges();
        }

        private void AwardCrystals(int userId, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            RegenerateHeartsIfNeeded(user);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            user.Crystals += amount;
            _dbContext.SaveChanges();
        }
    }
}
