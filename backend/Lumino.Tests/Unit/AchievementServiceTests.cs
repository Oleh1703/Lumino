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

        // не має бути "5 Lessons Completed"
        var five = dbContext.Achievements.FirstOrDefault(x => x.Title == "5 Lessons Completed");
        Assert.Null(five);

        // але "First Lesson" може бути
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

    [Fact]
    public void GrantHundredXp_ShouldGrant_WhenTotalScoreFromLessonsAndScenesReaches100()
    {
        var dbContext = TestDbContextFactory.Create();

        var userId = 10;

        // уроки: best score = 10
        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = userId,
            LessonId = 1,
            Score = 10,
            TotalQuestions = 10,
            CompletedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc)
        });

        // сцени: 18 completed * 5 = 90 => 10 + 90 = 100
        for (int i = 1; i <= 18; i++)
        {
            dbContext.SceneAttempts.Add(new SceneAttempt
            {
                UserId = userId,
                SceneId = i,
                IsCompleted = true,
                CompletedAt = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc)
            });
        }

        dbContext.SaveChanges();

        var service = new AchievementService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        service.CheckAndGrantAchievements(userId, 10, 10);

        var a = dbContext.Achievements.FirstOrDefault(x => x.Title == "100 XP");
        Assert.NotNull(a);

        var ua = dbContext.UserAchievements.FirstOrDefault(x => x.UserId == userId && x.AchievementId == a!.Id);
        Assert.NotNull(ua);
    }

    [Fact]
    public void GrantHundredXp_ShouldNotGrant_WhenTotalScoreLessThan100()
    {
        var dbContext = TestDbContextFactory.Create();

        var userId = 10;

        // уроки: best score = 4
        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = userId,
            LessonId = 1,
            Score = 4,
            TotalQuestions = 10,
            CompletedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc)
        });

        // сцени: 19 completed * 5 = 95 => 4 + 95 = 99
        for (int i = 1; i <= 19; i++)
        {
            dbContext.SceneAttempts.Add(new SceneAttempt
            {
                UserId = userId,
                SceneId = i,
                IsCompleted = true,
                CompletedAt = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc)
            });
        }

        dbContext.SaveChanges();

        var service = new AchievementService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        service.CheckAndGrantAchievements(userId, 4, 10);

        var a = dbContext.Achievements.FirstOrDefault(x => x.Title == "100 XP");
        Assert.Null(a);
    }
    [Fact]
    public void GrantDailyGoal_ShouldGrant_WhenTodayScoreMeetsTarget()
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

        // Сьогодні passed урок на 100% (Score=2) -> виконує DailyGoalScoreTarget=2
        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = userId,
            LessonId = 1,
            Score = 2,
            TotalQuestions = 2,
            CompletedAt = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var service = new AchievementService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, DailyGoalScoreTarget = 2 })
        );

        service.CheckAndGrantAchievements(userId, 2, 2);

        var daily = dbContext.Achievements.FirstOrDefault(x => x.Title == "Daily Goal");
        Assert.NotNull(daily);

        var userDaily = dbContext.UserAchievements.FirstOrDefault(x => x.UserId == userId && x.AchievementId == daily!.Id);
        Assert.NotNull(userDaily);
    }

    [Fact]
    public void GrantStreakSeven_ShouldGrant_WhenSevenConsecutiveDays()
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

        // 7 днів підряд passed урок (може бути один і той самий LessonId)
        for (int i = 0; i < 7; i++)
        {
            dbContext.LessonResults.Add(new LessonResult
            {
                UserId = userId,
                LessonId = 1,
                Score = 1,
                TotalQuestions = 1,
                CompletedAt = new DateTime(2026, 2, 1 + i, 10, 0, 0, DateTimeKind.Utc)
            });
        }

        dbContext.SaveChanges();

        var service = new AchievementService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 7, 12, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        service.CheckAndGrantAchievements(userId, 1, 1);

        var streak7 = dbContext.Achievements.FirstOrDefault(x => x.Title == "Streak 7");
        Assert.NotNull(streak7);

        var userStreak7 = dbContext.UserAchievements.FirstOrDefault(x => x.UserId == userId && x.AchievementId == streak7!.Id);
        Assert.NotNull(userStreak7);
    }


    [Fact]
    public void GrantDailyGoal_ShouldNotDuplicate_WhenCalledTwice()
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

        // Today: passed lesson => daily goal reached (target = 1)
        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = userId,
            LessonId = 1,
            Score = 1,
            TotalQuestions = 1,
            CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var service = new AchievementService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 12, 12, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, DailyGoalScoreTarget = 1 })
        );

        service.CheckAndGrantAchievements(userId, 1, 1);
        service.CheckAndGrantAchievements(userId, 1, 1);

        var dailyGoal = dbContext.Achievements.FirstOrDefault(x => x.Title == "Daily Goal");
        Assert.NotNull(dailyGoal);

        var count = dbContext.UserAchievements.Count(x => x.UserId == userId && x.AchievementId == dailyGoal!.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public void GrantStreakThirty_ShouldGrant_WhenThirtyConsecutiveDaysFromScenes()
    {
        var dbContext = TestDbContextFactory.Create();

        var userId = 10;

        // 30 consecutive study days via completed scenes
        for (int i = 0; i < 30; i++)
        {
            dbContext.SceneAttempts.Add(new SceneAttempt
            {
                UserId = userId,
                SceneId = 1000 + i,
                IsCompleted = true,
                Score = 1,
                CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc).Date.AddDays(-i).AddHours(10)
            });
        }

        dbContext.SaveChanges();

        var service = new AchievementService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 12, 12, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        service.CheckAndGrantSceneAchievements(userId);

        var streak30 = dbContext.Achievements.FirstOrDefault(x => x.Title == "Streak 30");
        Assert.NotNull(streak30);

        var userStreak30 = dbContext.UserAchievements.FirstOrDefault(x => x.UserId == userId && x.AchievementId == streak30!.Id);
        Assert.NotNull(userStreak30);
    }

}
