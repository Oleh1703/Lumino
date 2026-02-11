using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class AchievementServiceTests
{
    [Fact]
    public void GrantFiveLessons_ShouldNotGrant_WhenSameLessonPassedManyTimes()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Basics",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson 1",
            Theory = "T",
            Order = 1
        });

        var userId = 10;

        // 5 разів passed один і той самий урок
        dbContext.LessonResults.AddRange(
            new LessonResult { UserId = userId, LessonId = 1, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 1, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 1, 11, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 1, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 1, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 1, 13, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 1, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 1, 14, 0, 0, DateTimeKind.Utc) }
        );

        dbContext.SaveChanges();

        var service = new AchievementService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 1, 15, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        service.CheckAndGrantAchievements(userId, 4, 4);

        // ✅ не має бути "5 Lessons Completed"
        var five = dbContext.Achievements.FirstOrDefault(x => x.Title == "5 Lessons Completed");
        Assert.Null(five);

        // ✅ але "First Lesson" може бути
        var first = dbContext.Achievements.FirstOrDefault(x => x.Title == "First Lesson");
        Assert.NotNull(first);
    }

    [Fact]
    public void GrantFiveLessons_ShouldGrant_WhenFiveDistinctLessonsPassed()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Basics",
            Order = 1
        });

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "T", Order = 1 },
            new Lesson { Id = 2, TopicId = 1, Title = "L2", Theory = "T", Order = 2 },
            new Lesson { Id = 3, TopicId = 1, Title = "L3", Theory = "T", Order = 3 },
            new Lesson { Id = 4, TopicId = 1, Title = "L4", Theory = "T", Order = 4 },
            new Lesson { Id = 5, TopicId = 1, Title = "L5", Theory = "T", Order = 5 }
        );

        var userId = 10;

        // 5 різних уроків passed (80%+)
        dbContext.LessonResults.AddRange(
            new LessonResult { UserId = userId, LessonId = 1, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 2, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 3, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 3, 10, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 4, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 4, 10, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 5, Score = 4, TotalQuestions = 4, CompletedAt = new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc) }
        );

        dbContext.SaveChanges();

        var service = new AchievementService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 5, 12, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        service.CheckAndGrantAchievements(userId, 4, 4);

        var five = dbContext.Achievements.FirstOrDefault(x => x.Title == "5 Lessons Completed");
        Assert.NotNull(five);

        var userFive = dbContext.UserAchievements.FirstOrDefault(x => x.UserId == userId && x.AchievementId == five!.Id);
        Assert.NotNull(userFive);
    }
}
