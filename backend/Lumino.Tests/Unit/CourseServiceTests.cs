﻿using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class CourseServiceTests
{
    [Fact]
    public void GetPublishedCourses_ReturnsOnlyPublished()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.AddRange(
            new Course { Id = 1, Title = "Published", Description = "D1", IsPublished = true },
            new Course { Id = 2, Title = "Hidden", Description = "D2", IsPublished = false }
        );

        dbContext.SaveChanges();

        var service = new CourseService(dbContext);

        var result = service.GetPublishedCourses();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Published", result[0].Title);
        Assert.Equal("D1", result[0].Description);
    }

    [Fact]
    public void GetPublishedCourses_WhenNoPublished_ReturnsEmpty()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course { Id = 1, Title = "Hidden", Description = "D", IsPublished = false });
        dbContext.SaveChanges();

        var service = new CourseService(dbContext);

        var result = service.GetPublishedCourses();

        Assert.Empty(result);
    }
}
