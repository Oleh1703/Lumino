using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests.Unit
{
    public class UserEconomyServiceHeartsRegenTests
    {
        [Fact]
        public void RefreshHearts_ShouldRegenerateHearts_ByTimeInterval()
        {
            // arrange
            var dbContext = TestDbContextFactory.Create();

            var user = new User
            {
                Id = 1,
                Email = "test@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Hearts = 0,
                HeartsUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-61),
                Crystals = 0,
                Theme = "light"
            };

            dbContext.Users.Add(user);
            dbContext.SaveChanges();

            var settings = Options.Create(new LearningSettings
            {
                HeartsMax = 5,
                HeartRegenMinutes = 30
            });

            var service = new UserEconomyService(dbContext, settings);

            // act
            service.RefreshHearts(user.Id);

            // assert
            var updated = dbContext.Users.First(x => x.Id == user.Id);
            Assert.Equal(2, updated.Hearts);
            Assert.NotNull(updated.HeartsUpdatedAtUtc);
        }

        [Fact]
        public void RefreshHearts_ShouldNotExceedMax()
        {
            // arrange
            var dbContext = TestDbContextFactory.Create();

            var user = new User
            {
                Id = 2,
                Email = "max@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Hearts = 4,
                HeartsUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-200),
                Crystals = 0,
                Theme = "light"
            };

            dbContext.Users.Add(user);
            dbContext.SaveChanges();

            var settings = Options.Create(new LearningSettings
            {
                HeartsMax = 5,
                HeartRegenMinutes = 30
            });

            var service = new UserEconomyService(dbContext, settings);

            // act
            service.RefreshHearts(user.Id);

            // assert
            var updated = dbContext.Users.First(x => x.Id == user.Id);
            Assert.Equal(5, updated.Hearts);
        }
    }
}
