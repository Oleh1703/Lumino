using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests.Unit
{
    public class UserProfileEconomyFieldsTests
    {
        [Fact]
        public void GetCurrentUser_WhenHeartsFull_ShouldReturnNoNextHeartTimer()
        {
            // arrange
            var dbContext = TestDbContextFactory.Create();

            dbContext.Users.Add(new User
            {
                Id = 1,
                Email = "full@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Hearts = 5,
                HeartsUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                Crystals = 10,
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
            Assert.Equal(5, result.HeartsMax);
            Assert.Equal(30, result.HeartRegenMinutes);
            Assert.Equal(10, result.CrystalCostPerHeart);
            Assert.Null(result.NextHeartAtUtc);
            Assert.Equal(0, result.NextHeartInSeconds);
        }

        [Fact]
        public void GetCurrentUser_WhenHeartsNotFull_ShouldReturnNextHeartTimer()
        {
            // arrange
            var dbContext = TestDbContextFactory.Create();

            dbContext.Users.Add(new User
            {
                Id = 2,
                Email = "notfull@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Hearts = 3,
                HeartsUpdatedAtUtc = DateTime.UtcNow,
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
            var result = service.GetCurrentUser(2);

            // assert
            Assert.Equal(5, result.HeartsMax);
            Assert.NotNull(result.NextHeartAtUtc);
            Assert.True(result.NextHeartInSeconds > 0);
            Assert.True(result.NextHeartInSeconds <= 1800);
        }

        private class FakeUpdateProfileRequestValidator : IUpdateProfileRequestValidator
        {
            public void Validate(UpdateProfileRequest request)
            {
            }
        }
    }
}
