using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class LearningPathServiceTests
{
    [Fact]
    public void GetMyCoursePath_WhenNoResults_FirstLessonUnlocked_OthersLocked()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedCourse(dbContext);

        dbContext.SaveChanges();

        var service = new LearningPathService(
            dbContext,
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var result = service.GetMyCoursePath(1, 1);

        var lessons = result.Topics.SelectMany(x => x.Lessons).OrderBy(x => x.Order).ToList();

        Assert.Equal(3, lessons.Count);

        Assert.True(lessons[0].IsUnlocked);
        Assert.False(lessons[0].IsPassed);

        Assert.False(lessons[1].IsUnlocked);
        Assert.False(lessons[2].IsUnlocked);

        var progress = dbContext.UserLessonProgresses
            .Where(x => x.UserId == 1)
            .OrderBy(x => x.LessonId)
            .ToList();

        Assert.Equal(3, progress.Count);

        Assert.True(progress[0].IsUnlocked);
        Assert.False(progress[0].IsCompleted);

        Assert.False(progress[1].IsUnlocked);
        Assert.False(progress[2].IsUnlocked);
    }

    [Fact]
    public void GetMyCoursePath_WhenFirstLessonPassed_SecondUnlocked()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedCourse(dbContext);

        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = 1,
            LessonId = 1,
            Score = 4,
            TotalQuestions = 4,
            CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
            MistakesJson = "[]"
        });

        dbContext.SaveChanges();

        var service = new LearningPathService(
            dbContext,
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var result = service.GetMyCoursePath(1, 1);

        var lessons = result.Topics.SelectMany(x => x.Lessons).OrderBy(x => x.Order).ToList();

        Assert.True(lessons[0].IsUnlocked);
        Assert.True(lessons[0].IsPassed);

        Assert.True(lessons[1].IsUnlocked);
        Assert.False(lessons[1].IsPassed);

        Assert.False(lessons[2].IsUnlocked);

        var progress = dbContext.UserLessonProgresses
            .Where(x => x.UserId == 1)
            .OrderBy(x => x.LessonId)
            .ToList();

        Assert.Equal(3, progress.Count);

        Assert.True(progress[0].IsUnlocked);
        Assert.True(progress[0].IsCompleted);

        Assert.True(progress[1].IsUnlocked);
        Assert.False(progress[1].IsCompleted);

        Assert.False(progress[2].IsUnlocked);
    }

    private static void SeedCourse(Lumino.Api.Data.LuminoDbContext dbContext)
    {
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
            new Lesson { Id = 3, TopicId = 1, Title = "L3", Theory = "T3", Order = 3 }
        );
    }
}
