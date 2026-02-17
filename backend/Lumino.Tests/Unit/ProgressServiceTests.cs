﻿using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class ProgressServiceTests
{
    [Fact]
    public void GetMyProgress_WhenNoProgressAndNoResults_ReturnsZeros()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 9, 12, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var service = new ProgressService(
            dbContext,
            dateTimeProvider,
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var result = service.GetMyProgress(1);

        Assert.Equal(0, result.CompletedLessons);
        Assert.Equal(0, result.TotalScore);
        Assert.Equal(now, result.LastUpdatedAt);

        Assert.Equal(0, result.TotalLessons);
        Assert.Equal(0, result.CompletedDistinctLessons);
        Assert.Equal(0, result.CompletionPercent);

        Assert.Equal(0, result.CurrentStreakDays);
        Assert.Null(result.LastStudyAt);
    }

    [Fact]
    public void GetMyProgress_WhenHasData_CalculatesCompletionAndStreak()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            Description = "Demo",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Basics",
            Order = 1
        });

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "T1", Order = 1 },
            new Lesson { Id = 2, TopicId = 1, Title = "L2", Theory = "T2", Order = 2 },
            new Lesson { Id = 3, TopicId = 1, Title = "L3", Theory = "T3", Order = 3 },
            new Lesson { Id = 4, TopicId = 1, Title = "L4", Theory = "T4", Order = 4 }
        );

        dbContext.UserProgresses.Add(new UserProgress
        {
            UserId = 1,
            CompletedLessons = 2,
            TotalScore = 55,
            LastUpdatedAt = new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc)
        });

        // Passing 80%
        // L1: 4/4 passed
        // L2: 3/4 NOT passed (75%)
        // L3: 4/4 passed
        dbContext.LessonResults.AddRange(
            new LessonResult
            {
                UserId = 1,
                LessonId = 1,
                Score = 4,
                TotalQuestions = 4,
                CompletedAt = new DateTime(2026, 2, 7, 10, 0, 0, DateTimeKind.Utc)
            },
            new LessonResult
            {
                UserId = 1,
                LessonId = 2,
                Score = 3,
                TotalQuestions = 4,
                CompletedAt = new DateTime(2026, 2, 8, 10, 0, 0, DateTimeKind.Utc)
            },
            new LessonResult
            {
                UserId = 1,
                LessonId = 3,
                Score = 4,
                TotalQuestions = 4,
                CompletedAt = new DateTime(2026, 2, 9, 10, 0, 0, DateTimeKind.Utc)
            }
        );

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 9, 12, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var service = new ProgressService(
            dbContext,
            dateTimeProvider,
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var result = service.GetMyProgress(1);

        Assert.Equal(2, result.CompletedLessons);
        Assert.Equal(55, result.TotalScore);

        Assert.Equal(4, result.TotalLessons);
        Assert.Equal(2, result.CompletedDistinctLessons);
        Assert.Equal(50, result.CompletionPercent);

        // ✅ streak рахуємо ТІЛЬКИ по passed-днях:
        // passed: 07.02, 09.02 -> між ними є день без passed, тому streak = 1
        Assert.Equal(1, result.CurrentStreakDays);
        Assert.Equal(new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), result.LastStudyAt);
    }

    [Fact]
    public void GetMyProgress_WhenLastStudyOlderThanYesterday_StreakIsZero()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            Description = "Demo",
            IsPublished = true
        });

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
            Title = "L1",
            Theory = "T1",
            Order = 1
        });

        dbContext.UserProgresses.Add(new UserProgress
        {
            UserId = 1,
            CompletedLessons = 1,
            TotalScore = 10,
            LastUpdatedAt = new DateTime(2026, 2, 7, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = 1,
            LessonId = 1,
            Score = 4,
            TotalQuestions = 4,
            CompletedAt = new DateTime(2026, 2, 7, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 9, 12, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var service = new ProgressService(
            dbContext,
            dateTimeProvider,
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var result = service.GetMyProgress(1);

        Assert.Equal(0, result.CurrentStreakDays);
        Assert.Equal(new DateTime(2026, 2, 7, 0, 0, 0, DateTimeKind.Utc), result.LastStudyAt);
    }
    [Fact]
    public void GetMyDailyGoal_WhenHasStudyToday_CalculatesTodayScoreAndRemaining()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            Description = "Demo",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Basics",
            Order = 1
        });

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "T1", Order = 1 },
            new Lesson { Id = 2, TopicId = 1, Title = "L2", Theory = "T2", Order = 2 }
        );

        var now = new DateTime(2026, 2, 9, 12, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        // Passing 80%:
        // today: L1 (6/6 passed), L2 (3/4 not passed)
        // today: Scene1 completed (5/5)
        dbContext.LessonResults.AddRange(
            new LessonResult
            {
                UserId = 1,
                LessonId = 1,
                Score = 6,
                TotalQuestions = 6,
                CompletedAt = new DateTime(2026, 2, 9, 9, 0, 0, DateTimeKind.Utc)
            },
            new LessonResult
            {
                UserId = 1,
                LessonId = 2,
                Score = 3,
                TotalQuestions = 4,
                CompletedAt = new DateTime(2026, 2, 9, 10, 0, 0, DateTimeKind.Utc)
            },
            new LessonResult
            {
                UserId = 1,
                LessonId = 2,
                Score = 4,
                TotalQuestions = 4,
                CompletedAt = new DateTime(2026, 2, 8, 10, 0, 0, DateTimeKind.Utc)
            }
        );

        dbContext.SceneAttempts.AddRange(
            new SceneAttempt
            {
                UserId = 1,
                SceneId = 1,
                IsCompleted = true,
                Score = 5,
                TotalQuestions = 5,
                CompletedAt = new DateTime(2026, 2, 9, 11, 0, 0, DateTimeKind.Utc)
            },
            new SceneAttempt
            {
                UserId = 1,
                SceneId = 2,
                IsCompleted = false,
                Score = 10,
                TotalQuestions = 10,
                CompletedAt = new DateTime(2026, 2, 9, 11, 30, 0, DateTimeKind.Utc)
            }
        );

        dbContext.SaveChanges();

        var service = new ProgressService(
            dbContext,
            dateTimeProvider,
            Options.Create(new LearningSettings
            {
                PassingScorePercent = 80,
                DailyGoalScoreTarget = 12
            })
        );

        var result = service.GetMyDailyGoal(1);

        Assert.Equal(new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc), result.DateUtc);
        Assert.Equal(12, result.TargetScore);

        // Only passed lessons + completed scenes today:
        // L1 passed (6) + Scene1 completed (5) = 11
        Assert.Equal(11, result.TodayScore);
        Assert.Equal(1, result.RemainingScore);
        Assert.False(result.IsGoalMet);

        Assert.Equal(1, result.TodayPassedLessons);
        Assert.Equal(1, result.TodayCompletedScenes);
    }


}
