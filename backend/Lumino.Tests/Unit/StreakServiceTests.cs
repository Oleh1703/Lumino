using Lumino.Api.Application.Services;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumino.Tests;

public class StreakServiceTests
{
    [Fact]
    public void RegisterLessonActivity_FirstTime_CreatesStreakAndActivity()
    {
        var dbContext = TestDbContextFactory.Create();
        var dt = new FixedDateTimeProvider(new DateTime(2026, 2, 26, 10, 0, 0, DateTimeKind.Utc));

        var service = new StreakService(dbContext, dt);

        var user = new User
        {
            Email = "a@a.com",
            PasswordHash = "hash",
            Role = Lumino.Api.Domain.Enums.Role.User,
            CreatedAt = dt.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        service.RegisterLessonActivity(user.Id);

        var streak = dbContext.UserStreaks.FirstOrDefault(x => x.UserId == user.Id);
        Assert.NotNull(streak);
        Assert.Equal(1, streak!.CurrentStreak);
        Assert.Equal(1, streak.BestStreak);
        Assert.Equal(dt.UtcNow.Date, streak.LastActivityDateUtc.Date);

        var activity = dbContext.UserDailyActivities.FirstOrDefault(x => x.UserId == user.Id && x.DateUtc == dt.UtcNow.Date);
        Assert.NotNull(activity);
    }

    [Fact]
    public void RegisterLessonActivity_SameDay_DoesNotChangeStreak()
    {
        var dbContext = TestDbContextFactory.Create();
        var dt = new FixedDateTimeProvider(new DateTime(2026, 2, 26, 10, 0, 0, DateTimeKind.Utc));

        var user = new User
        {
            Email = "b@b.com",
            PasswordHash = "hash",
            Role = Lumino.Api.Domain.Enums.Role.User,
            CreatedAt = dt.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        var service = new StreakService(dbContext, dt);

        service.RegisterLessonActivity(user.Id);
        service.RegisterLessonActivity(user.Id);

        var streak = dbContext.UserStreaks.First(x => x.UserId == user.Id);
        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(1, streak.BestStreak);

        var activities = dbContext.UserDailyActivities.Where(x => x.UserId == user.Id).ToList();
        Assert.Single(activities);
    }

    [Fact]
    public void RegisterLessonActivity_NextDay_IncrementsStreak()
    {
        var dbContext = TestDbContextFactory.Create();
        var dt = new FixedDateTimeProvider(new DateTime(2026, 2, 26, 10, 0, 0, DateTimeKind.Utc));

        var user = new User
        {
            Email = "c@c.com",
            PasswordHash = "hash",
            Role = Lumino.Api.Domain.Enums.Role.User,
            CreatedAt = dt.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        var service = new StreakService(dbContext, dt);

        service.RegisterLessonActivity(user.Id);

        dt.UtcNow = dt.UtcNow.AddDays(1);

        service.RegisterLessonActivity(user.Id);

        var streak = dbContext.UserStreaks.First(x => x.UserId == user.Id);
        Assert.Equal(2, streak.CurrentStreak);
        Assert.Equal(2, streak.BestStreak);
    }

    [Fact]
    public void RegisterLessonActivity_Gap_ResetsStreak()
    {
        var dbContext = TestDbContextFactory.Create();
        var dt = new FixedDateTimeProvider(new DateTime(2026, 2, 26, 10, 0, 0, DateTimeKind.Utc));

        var user = new User
        {
            Email = "d@d.com",
            PasswordHash = "hash",
            Role = Lumino.Api.Domain.Enums.Role.User,
            CreatedAt = dt.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        var service = new StreakService(dbContext, dt);

        service.RegisterLessonActivity(user.Id);

        dt.UtcNow = dt.UtcNow.AddDays(2);

        service.RegisterLessonActivity(user.Id);

        var streak = dbContext.UserStreaks.First(x => x.UserId == user.Id);
        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(1, streak.BestStreak);
    }
}
