using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;
using Lumino.Api.Application.DTOs;

namespace Lumino.Tests.Unit
{
    public class UserProfileStreakFieldsTests
    {
        [Fact]
        public void GetCurrentUser_WhenNoStreak_ShouldReturnZeros()
        {
            // arrange
            var dbContext = TestDbContextFactory.Create();

            dbContext.Users.Add(new User
            {
                Id = 1,
                Email = "streak0@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Hearts = 5,
                Crystals = 0,
                Theme = "light"
            });

            dbContext.SaveChanges();

            var settings = Options.Create(new LearningSettings
            {
                HeartsMax = 5,
                HeartRegenMinutes = 30,
                CrystalCostPerHeart = 10
            });

            var service = new UserService(dbContext, new FakeUpdateProfileRequestValidator(), settings);

            // act
            var result = service.GetCurrentUser(1);

            // assert
            Assert.Equal(0, result.CurrentStreakDays);
            Assert.Equal(0, result.BestStreakDays);
        }

        [Fact]
        public void GetCurrentUser_WhenStreakOld_ShouldResetCurrent()
        {
            // arrange
            var dbContext = TestDbContextFactory.Create();
            var todayUtc = DateTime.UtcNow.Date;

            dbContext.Users.Add(new User
            {
                Id = 2,
                Email = "streak@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Hearts = 5,
                Crystals = 0,
                Theme = "light"
            });

            dbContext.UserStreaks.Add(new UserStreak
            {
                UserId = 2,
                CurrentStreak = 7,
                BestStreak = 10,
                LastActivityDateUtc = todayUtc.AddDays(-3)
            });

            dbContext.SaveChanges();

            var settings = Options.Create(new LearningSettings
            {
                HeartsMax = 5,
                HeartRegenMinutes = 30,
                CrystalCostPerHeart = 10
            });

            var service = new UserService(dbContext, new FakeUpdateProfileRequestValidator(), settings);

            // act
            var result = service.GetCurrentUser(2);

            // assert
            Assert.Equal(0, result.CurrentStreakDays);
            Assert.Equal(10, result.BestStreakDays);
        }

        private class FakeUpdateProfileRequestValidator : IUpdateProfileRequestValidator
        {
            public void Validate(UpdateProfileRequest request)
            {
            }
        }
    }
}