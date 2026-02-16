using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class NextActivityServiceTests
{
    [Fact]
    public void GetNext_WhenHasDueVocabulary_ReturnsVocabularyReview()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.VocabularyItems.Add(new VocabularyItem
        {
            Id = 1,
            Word = "hello",
            Translation = "привіт",
            Example = null
        });

        dbContext.UserVocabularies.Add(new UserVocabulary
        {
            Id = 10,
            UserId = 5,
            VocabularyItemId = 1,
            AddedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            LastReviewedAt = null,
            NextReviewAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            ReviewCount = 0
        });

        SeedLessons(dbContext);
        SeedScenes(dbContext);

        dbContext.SaveChanges();

        var service = new NextActivityService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var next = service.GetNext(5);

        Assert.NotNull(next);
        Assert.Equal("VocabularyReview", next!.Type);
        Assert.Equal(10, next.UserVocabularyId);
        Assert.Equal(1, next.VocabularyItemId);
    }

    [Fact]
    public void GetNext_WhenNoDueVocabulary_ReturnsNextUnpassedLesson()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedLessons(dbContext);
        SeedScenes(dbContext);

        // Mark lesson 1 as passed
        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 5,
            LessonId = 1,
            Score = 8,
            TotalQuestions = 10,
            MistakesJson = "[]",
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var service = new NextActivityService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var next = service.GetNext(5);

        Assert.NotNull(next);
        Assert.Equal("Lesson", next!.Type);
        Assert.Equal(2, next.LessonId);
        Assert.Equal(1, next.TopicId);

        // ✅ доробка №4: перевіряємо, що lesson 2 реально unlocked
        var p1 = dbContext.UserLessonProgresses.FirstOrDefault(x => x.UserId == 5 && x.LessonId == 1);
        var p2 = dbContext.UserLessonProgresses.FirstOrDefault(x => x.UserId == 5 && x.LessonId == 2);

        Assert.NotNull(p1);
        Assert.NotNull(p2);

        Assert.True(p1!.IsUnlocked);
        Assert.True(p1.IsCompleted);

        Assert.True(p2!.IsUnlocked);
        Assert.False(p2.IsCompleted);
    }

    [Fact]
    public void GetNext_WhenAllLessonsPassed_ReturnsNextUncompletedScene()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedLessons(dbContext);
        SeedScenes(dbContext);

        // Mark lesson 1 and lesson 2 as passed
        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 5,
            LessonId = 1,
            Score = 8,
            TotalQuestions = 10,
            MistakesJson = "[]",
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 2,
            UserId = 5,
            LessonId = 2,
            Score = 8,
            TotalQuestions = 10,
            MistakesJson = "[]",
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        // Mark scene 1 completed
        dbContext.SceneAttempts.Add(new SceneAttempt
        {
            Id = 1,
            UserId = 5,
            SceneId = 1,
            IsCompleted = true,
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var service = new NextActivityService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var next = service.GetNext(5);

        Assert.NotNull(next);
        Assert.Equal("Scene", next!.Type);
        Assert.Equal(2, next.SceneId);
    }

    [Fact]
    public void GetNext_WhenSceneOrderNotSequential_ShouldUsePositionNotRawOrder()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedLessons(dbContext);

        dbContext.Scenes.AddRange(
            new Scene { Id = 1, Title = "Scene 1", Description = "D", SceneType = "Dialog", Order = 10 },
            new Scene { Id = 2, Title = "Scene 2", Description = "D", SceneType = "Dialog", Order = 20 }
        );

        // Mark lesson 1 and lesson 2 as passed
        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 5,
            LessonId = 1,
            Score = 8,
            TotalQuestions = 10,
            MistakesJson = "[]",
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 2,
            UserId = 5,
            LessonId = 2,
            Score = 8,
            TotalQuestions = 10,
            MistakesJson = "[]",
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var service = new NextActivityService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var next = service.GetNext(5);

        Assert.NotNull(next);
        Assert.Equal("Scene", next!.Type);
        Assert.Equal(1, next.SceneId);
    }

    [Fact]
    public void GetNext_WhenNothingToDo_ReturnsNull()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedLessons(dbContext);
        SeedScenes(dbContext);

        // Mark lesson 1 and lesson 2 as passed
        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 5,
            LessonId = 1,
            Score = 8,
            TotalQuestions = 10,
            MistakesJson = "[]",
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 2,
            UserId = 5,
            LessonId = 2,
            Score = 8,
            TotalQuestions = 10,
            MistakesJson = "[]",
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        // Mark scenes 1 and 2 completed
        dbContext.SceneAttempts.Add(new SceneAttempt
        {
            Id = 1,
            UserId = 5,
            SceneId = 1,
            IsCompleted = true,
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SceneAttempts.Add(new SceneAttempt
        {
            Id = 2,
            UserId = 5,
            SceneId = 2,
            IsCompleted = true,
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var service = new NextActivityService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var next = service.GetNext(5);

        Assert.Null(next);
    }

    [Fact]
    public void GetNext_WhenHasActiveCourse_ReturnsLessonFromActiveCourse()
    {
        var dbContext = TestDbContextFactory.Create();

        // Course 1 (published) - щоб перевірити що активний курс має пріоритет
        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            Description = "Desc",
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
            Title = "Lesson 1",
            Theory = "T",
            Order = 1
        });

        // Course 2 (published) - активний
        dbContext.Courses.Add(new Course
        {
            Id = 2,
            Title = "German A1",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 2,
            CourseId = 2,
            Title = "Start",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 3,
            TopicId = 2,
            Title = "DE Lesson 1",
            Theory = "T",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 4,
            TopicId = 2,
            Title = "DE Lesson 2",
            Theory = "T",
            Order = 2
        });

        SeedScenes(dbContext);

        dbContext.UserCourses.Add(new UserCourse
        {
            Id = 1,
            UserId = 5,
            CourseId = 2,
            IsActive = true,
            LastLessonId = 3,
            StartedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            LastOpenedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            Id = 1,
            UserId = 5,
            LessonId = 3,
            IsUnlocked = true,
            IsCompleted = false,
            BestScore = 0,
            LastAttemptAt = null
        });

        dbContext.SaveChanges();

        var service = new NextActivityService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc)),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var next = service.GetNext(5);

        Assert.NotNull(next);
        Assert.Equal("Lesson", next!.Type);
        Assert.Equal(3, next.LessonId);
        Assert.Equal(2, next.TopicId);

        // перевірка, що повернений урок unlocked
        var p = dbContext.UserLessonProgresses.FirstOrDefault(x => x.UserId == 5 && x.LessonId == 3);
        Assert.NotNull(p);
        Assert.True(p!.IsUnlocked);
    }

    private static void SeedLessons(Lumino.Api.Data.LuminoDbContext dbContext)
    {
        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            Description = "Desc",
            IsPublished = true
        });

        // Topic.Order важливий
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

        dbContext.Lessons.Add(new Lesson
        {
            Id = 2,
            TopicId = 1,
            Title = "Lesson 2",
            Theory = "T",
            Order = 2
        });
    }

    private static void SeedScenes(Lumino.Api.Data.LuminoDbContext dbContext)
    {
        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "D",
            SceneType = "Dialogue"
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 2,
            Title = "Scene 2",
            Description = "D",
            SceneType = "Dialogue"
        });
    }
}
