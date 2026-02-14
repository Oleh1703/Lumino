﻿using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Xunit;

namespace Lumino.Tests;

public class LessonServiceTests
{
    [Fact]
    public void GetLessonsByTopic_WhenTopicNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new LessonService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetLessonsByTopic(999));
    }

    [Fact]
    public void GetLessonsByTopic_WhenCourseNotPublished_Throws()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            Description = "Demo",
            IsPublished = false
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Basics",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new LessonService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetLessonsByTopic(1));
    }

    [Fact]
    public void GetLessonsByTopic_ReturnsLessonsOrderedByOrder()
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
            new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "T1", Order = 2 },
            new Lesson { Id = 2, TopicId = 1, Title = "L2", Theory = "T2", Order = 1 },
            new Lesson { Id = 3, TopicId = 1, Title = "L3", Theory = "T3", Order = 3 }
        );

        dbContext.SaveChanges();

        var service = new LessonService(dbContext);

        var result = service.GetLessonsByTopic(1);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    [Fact]
    public void GetLessonById_WhenLessonNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new LessonService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetLessonById(10, 999));
    }

    [Fact]
    public void GetLessonById_WhenLocked_ThrowsForbidden()
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
            Title = "Lesson 1",
            Theory = "Theory",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new LessonService(dbContext);

        Assert.Throws<ForbiddenAccessException>(() => service.GetLessonById(10, 1));
    }

    [Fact]
    public void GetLessonById_WhenUnlocked_ReturnsLesson()
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
            Title = "Lesson 1",
            Theory = "Theory",
            Order = 1
        });

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 10,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false,
            BestScore = 0,
            LastAttemptAt = null
        });

        dbContext.SaveChanges();

        var service = new LessonService(dbContext);

        var result = service.GetLessonById(10, 1);

        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.TopicId);
        Assert.Equal("Lesson 1", result.Title);
        Assert.Equal("Theory", result.Theory);
        Assert.Equal(1, result.Order);
    }
}
