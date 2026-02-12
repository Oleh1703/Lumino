﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class AdminLessonServiceTests
{
    [Fact]
    public void GetByTopic_ReturnsOrderedByOrder()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Topic",
            Order = 1
        });

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "T1", Order = 2 },
            new Lesson { Id = 2, TopicId = 1, Title = "L2", Theory = "T2", Order = 1 },
            new Lesson { Id = 3, TopicId = 1, Title = "L3", Theory = "T3", Order = 3 }
        );

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        var result = service.GetByTopic(1);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    [Fact]
    public void Create_AddsLesson_AndReturnsResponse()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Topic",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        var response = service.Create(new CreateLessonRequest
        {
            TopicId = 1,
            Title = "Lesson",
            Theory = "Theory",
            Order = 1
        });

        Assert.True(response.Id > 0);
        Assert.Equal(1, response.TopicId);
        Assert.Equal("Lesson", response.Title);
        Assert.Equal("Theory", response.Theory);
        Assert.Equal(1, response.Order);

        var saved = dbContext.Lessons.FirstOrDefault(x => x.Id == response.Id);
        Assert.NotNull(saved);
        Assert.Equal(1, saved!.TopicId);
        Assert.Equal("Lesson", saved.Title);
        Assert.Equal("Theory", saved.Theory);
        Assert.Equal(1, saved.Order);
    }

    [Fact]
    public void Create_NullRequest_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminLessonService(dbContext);

        Assert.Throws<ArgumentException>(() => service.Create(null!));
    }

    [Fact]
    public void Update_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminLessonService(dbContext);

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.Update(999, new UpdateLessonRequest
            {
                Title = "New",
                Theory = "NewTheory",
                Order = 2
            });
        });
    }

    [Fact]
    public void Update_UpdatesFields()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Old",
            Theory = "OldTheory",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        service.Update(1, new UpdateLessonRequest
        {
            Title = "New",
            Theory = "NewTheory",
            Order = 2
        });

        var updated = dbContext.Lessons.First(x => x.Id == 1);

        Assert.Equal("New", updated.Title);
        Assert.Equal("NewTheory", updated.Theory);
        Assert.Equal(2, updated.Order);
    }

    [Fact]
    public void Delete_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminLessonService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.Delete(999));
    }

    [Fact]
    public void Delete_RemovesLesson()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "ToDelete",
            Theory = "T",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        service.Delete(1);

        Assert.Empty(dbContext.Lessons);
    }
}
