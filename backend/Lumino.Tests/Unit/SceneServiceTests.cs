﻿using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class SceneServiceTests
{
    [Fact]
    public void MarkCompleted_FirstTime_ShouldCreateAttempt_AndCreateProgress_AndIncreaseTotalScore()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "scene@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        // 2 результати одного lesson -> береться Max(score)
        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 10,
            Score = 2,
            TotalQuestions = 5,
            CompletedAt = DateTime.UtcNow
        });

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 2,
            UserId = 1,
            LessonId = 10,
            Score = 3,
            TotalQuestions = 5,
            CompletedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var settings = Options.Create(new LearningSettings
        {
            SceneCompletionScore = 5,
            SceneUnlockEveryLessons = 1
        });

        var service = new SceneService(dbContext, dateTimeProvider, achievementService, settings);

        service.MarkCompleted(userId: 1, sceneId: 1);

        var attempts = dbContext.SceneAttempts.ToList();
        Assert.Single(attempts);

        Assert.Equal(1, attempts[0].UserId);
        Assert.Equal(1, attempts[0].SceneId);
        Assert.True(attempts[0].IsCompleted);
        Assert.Equal(now, attempts[0].CompletedAt);

        var progress = dbContext.UserProgresses.FirstOrDefault(x => x.UserId == 1);
        Assert.NotNull(progress);

        // lessonsScore = Max(2,3)=3
        // scenesScore = 1 * 5 = 5
        Assert.Equal(8, progress!.TotalScore);
        Assert.Equal(0, progress.CompletedLessons);
        Assert.Equal(now, progress.LastUpdatedAt);

        Assert.Equal(1, achievementService.SceneChecksCount);
    }

    [Fact]
    public void MarkCompleted_WhenSceneLocked_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "locked@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        // scene 2 -> requiredLessons = (2-1)*1 = 1
        dbContext.Scenes.Add(new Scene
        {
            Id = 2,
            Title = "Scene 2",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 11, 0, 0, DateTimeKind.Utc);

        var service = new SceneService(
            dbContext,
            new FixedDateTimeProvider(now),
            new FakeAchievementService(),
            Options.Create(new LearningSettings { SceneCompletionScore = 5, SceneUnlockEveryLessons = 1 })
        );

        Assert.Throws<ForbiddenAccessException>(() =>
        {
            service.MarkCompleted(userId: 1, sceneId: 2);
        });
    }

    [Fact]
    public void MarkCompleted_SecondTime_ShouldBeIdempotent_AndNotIncreaseScoreAgain()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "scene2@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 2,
            Title = "Scene 2",
            Description = "Desc",
            SceneType = "intro"
        });

        // щоб Scene 2 була unlocked: треба 1 passed lesson (80%+)
        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 100,
            Score = 8,
            TotalQuestions = 10,
            CompletedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 12, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var settings = Options.Create(new LearningSettings
        {
            PassingScorePercent = 80,
            SceneCompletionScore = 5,
            SceneUnlockEveryLessons = 1
        });

        var service = new SceneService(dbContext, dateTimeProvider, achievementService, settings);

        service.MarkCompleted(userId: 1, sceneId: 2);

        var score1 = dbContext.UserProgresses.First(x => x.UserId == 1).TotalScore;

        service.MarkCompleted(userId: 1, sceneId: 2);

        var attempts = dbContext.SceneAttempts.ToList();
        Assert.Single(attempts);

        var score2 = dbContext.UserProgresses.First(x => x.UserId == 1).TotalScore;

        Assert.Equal(score1, score2);
        Assert.Equal(1, achievementService.SceneChecksCount);
    }

    [Fact]
    public void GetCompletedScenes_ShouldReturnOnlyCompletedSceneIds()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "scene3@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "S1",
            Description = "D1",
            SceneType = "intro"
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 2,
            Title = "S2",
            Description = "D2",
            SceneType = "intro"
        });

        dbContext.SceneAttempts.Add(new SceneAttempt
        {
            Id = 1,
            UserId = 1,
            SceneId = 1,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow
        });

        dbContext.SceneAttempts.Add(new SceneAttempt
        {
            Id = 2,
            UserId = 1,
            SceneId = 2,
            IsCompleted = false,
            CompletedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 15, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var service = new SceneService(
            dbContext,
            dateTimeProvider,
            new FakeAchievementService(),
            Options.Create(new LearningSettings())
        );

        var list = service.GetCompletedScenes(userId: 1);

        Assert.Single(list);
        Assert.Contains(1, list);
        Assert.DoesNotContain(2, list);
    }

    private class CountingAchievementService : IAchievementService
    {
        public int SceneChecksCount { get; private set; }

        public void CheckAndGrantAchievements(int userId, int lessonScore, int totalQuestions) { }

        public void CheckAndGrantSceneAchievements(int userId)
        {
            SceneChecksCount++;
        }
    }
}
