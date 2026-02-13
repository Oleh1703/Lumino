using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Xunit;

namespace Lumino.Tests.Integration;

public class ExerciseServiceIntegrationTests
{
    [Fact]
    public void GetExercisesByLesson_LockedLesson_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Topic",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson",
            Theory = "Text",
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 1,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q",
            Data = "",
            CorrectAnswer = "hello",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new ExerciseService(dbContext);

        Assert.Throws<ForbiddenAccessException>(() =>
        {
            service.GetExercisesByLesson(10, 1);
        });
    }

    [Fact]
    public void GetExercisesByLesson_UnlockedLesson_ShouldReturnExercises()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Topic",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson",
            Theory = "Text",
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 1,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q",
            Data = "",
            CorrectAnswer = "hello",
            Order = 1
        });

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 10,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false,
            BestScore = 0,
            LastAttemptAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var service = new ExerciseService(dbContext);

        var list = service.GetExercisesByLesson(10, 1);

        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
    }
}
