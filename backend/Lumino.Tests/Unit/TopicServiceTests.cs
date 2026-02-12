﻿using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class TopicServiceTests
{
    [Fact]
    public void GetTopicsByCourse_WhenCourseNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new TopicService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetTopicsByCourse(999));
    }

    [Fact]
    public void GetTopicsByCourse_WhenCourseNotPublished_Throws()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = false
        });

        dbContext.SaveChanges();

        var service = new TopicService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetTopicsByCourse(1));
    }

    [Fact]
    public void GetTopicsByCourse_ReturnsOrderedByOrder()
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

        var service = new TopicService(dbContext);

        var result = service.GetTopicsByCourse(1);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal("T2", result[0].Title);
        Assert.Equal(1, result[0].Order);

        Assert.Equal(1, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }
}
