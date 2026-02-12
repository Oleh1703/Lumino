﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class AdminTopicServiceTests
{
    [Fact]
    public void GetByCourse_ReturnsOrderedByOrder()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Topics.AddRange(
            new Topic { Id = 1, CourseId = 1, Title = "T1", Order = 2 },
            new Topic { Id = 2, CourseId = 1, Title = "T2", Order = 1 },
            new Topic { Id = 3, CourseId = 1, Title = "T3", Order = 3 }
        );

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        var result = service.GetByCourse(1);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    [Fact]
    public void Create_AddsTopic_AndReturnsResponse()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        var response = service.Create(new CreateTopicRequest
        {
            CourseId = 1,
            Title = "Basics",
            Order = 1
        });

        Assert.True(response.Id > 0);
        Assert.Equal(1, response.CourseId);
        Assert.Equal("Basics", response.Title);
        Assert.Equal(1, response.Order);

        var saved = dbContext.Topics.FirstOrDefault(x => x.Id == response.Id);
        Assert.NotNull(saved);
        Assert.Equal(1, saved!.CourseId);
        Assert.Equal("Basics", saved.Title);
        Assert.Equal(1, saved.Order);
    }

    [Fact]
    public void Create_NullRequest_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminTopicService(dbContext);

        Assert.Throws<ArgumentException>(() => service.Create(null!));
    }

    [Fact]
    public void Update_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminTopicService(dbContext);

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.Update(999, new UpdateTopicRequest
            {
                Title = "New",
                Order = 10
            });
        });
    }

    [Fact]
    public void Update_UpdatesFields()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Old",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        service.Update(1, new UpdateTopicRequest
        {
            Title = "New",
            Order = 2
        });

        var updated = dbContext.Topics.First(x => x.Id == 1);

        Assert.Equal("New", updated.Title);
        Assert.Equal(2, updated.Order);
    }

    [Fact]
    public void Delete_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminTopicService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.Delete(999));
    }

    [Fact]
    public void Delete_RemovesTopic()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "ToDelete",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        service.Delete(1);

        Assert.Empty(dbContext.Topics);
    }
}
