﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class AdminCourseServiceTests
{
    [Fact]
    public void GetAll_ReturnsAllCourses()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.AddRange(
            new Course { Id = 1, Title = "A1", Description = "D1", IsPublished = true },
            new Course { Id = 2, Title = "A2", Description = "D2", IsPublished = false }
        );

        dbContext.SaveChanges();

        var service = new AdminCourseService(dbContext);

        var result = service.GetAll();

        Assert.Equal(2, result.Count);

        Assert.Contains(result, x => x.Id == 1 && x.Title == "A1" && x.Description == "D1" && x.IsPublished);
        Assert.Contains(result, x => x.Id == 2 && x.Title == "A2" && x.Description == "D2" && x.IsPublished == false);
    }

    [Fact]
    public void Create_AddsCourse_AndReturnsResponse()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminCourseService(dbContext);

        var response = service.Create(new CreateCourseRequest
        {
            Title = "English A1",
            Description = "Desc",
            IsPublished = true
        });

        Assert.True(response.Id > 0);
        Assert.Equal("English A1", response.Title);
        Assert.Equal("Desc", response.Description);
        Assert.True(response.IsPublished);

        var saved = dbContext.Courses.FirstOrDefault(x => x.Id == response.Id);
        Assert.NotNull(saved);
        Assert.Equal("English A1", saved!.Title);
        Assert.Equal("Desc", saved.Description);
        Assert.True(saved.IsPublished);
    }

    [Fact]
    public void Create_NullRequest_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminCourseService(dbContext);

        Assert.Throws<ArgumentException>(() => service.Create(null!));
    }

    [Fact]
    public void Update_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminCourseService(dbContext);

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.Update(999, new UpdateCourseRequest
            {
                Title = "T",
                Description = "D",
                IsPublished = true
            });
        });
    }

    [Fact]
    public void Update_UpdatesFields()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Old",
            Description = "OldDesc",
            IsPublished = false
        });

        dbContext.SaveChanges();

        var service = new AdminCourseService(dbContext);

        service.Update(1, new UpdateCourseRequest
        {
            Title = "New",
            Description = "NewDesc",
            IsPublished = true
        });

        var updated = dbContext.Courses.First(x => x.Id == 1);

        Assert.Equal("New", updated.Title);
        Assert.Equal("NewDesc", updated.Description);
        Assert.True(updated.IsPublished);
    }

    [Fact]
    public void Delete_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminCourseService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.Delete(999));
    }

    [Fact]
    public void Delete_RemovesCourse()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "ToDelete",
            Description = "D",
            IsPublished = true
        });

        dbContext.SaveChanges();

        var service = new AdminCourseService(dbContext);

        service.Delete(1);

        Assert.Empty(dbContext.Courses);
    }
}
