using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class ProfileServiceTests
{
    [Fact]
    public void GetWeeklyProgress_ReturnsTwoWeeksAndTotalPoints()
    {
        var dbContext = TestDbContextFactory.Create();

        // Monday, so current week starts at 2026-02-23
        var now = new DateTime(2026, 2, 23, 12, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        int userId = 1;

        // Current week: Mon + Tue
        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = userId,
            LessonId = 10,
            Score = 5,
            TotalQuestions = 10,
            CompletedAt = new DateTime(2026, 2, 23, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = userId,
            LessonId = 11,
            Score = 7,
            TotalQuestions = 10,
            CompletedAt = new DateTime(2026, 2, 24, 10, 0, 0, DateTimeKind.Utc)
        });

        // Previous week: Mon
        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = userId,
            LessonId = 12,
            Score = 3,
            TotalQuestions = 10,
            CompletedAt = new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var service = new ProfileService(dbContext, dateTimeProvider);

        var result = service.GetWeeklyProgress(userId);

        Assert.NotNull(result);
        Assert.Equal(7, result.CurrentWeek.Count);
        Assert.Equal(7, result.PreviousWeek.Count);

        Assert.Equal(5, result.CurrentWeek[0].Points); // 2026-02-23
        Assert.Equal(7, result.CurrentWeek[1].Points); // 2026-02-24
        Assert.Equal(0, result.CurrentWeek[2].Points);

        Assert.Equal(3, result.PreviousWeek[0].Points); // 2026-02-16
        Assert.Equal(0, result.PreviousWeek[1].Points);

        Assert.Equal(15, result.TotalPoints);
    }
}
