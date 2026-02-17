using Lumino.Api.Application.DTOs;
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

        var course = new Course
        {
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic1 = new Topic { CourseId = course.Id, Title = "T1", Order = 2 };
        var topic2 = new Topic { CourseId = course.Id, Title = "T2", Order = 1 };
        var topic3 = new Topic { CourseId = course.Id, Title = "T3", Order = 3 };

        dbContext.Topics.AddRange(topic1, topic2, topic3);

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        var result = service.GetByCourse(course.Id);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Order);
        Assert.Equal(2, result[1].Order);
        Assert.Equal(3, result[2].Order);
        Assert.Equal("T2", result[0].Title);
        Assert.Equal("T1", result[1].Title);
        Assert.Equal("T3", result[2].Title);
    }

    [Fact]
    public void Create_AddsTopic_AndReturnsResponse()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course
        {
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        };

        dbContext.Courses.Add(course);

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        var response = service.Create(new CreateTopicRequest
        {
            CourseId = course.Id,
            Title = "Basics",
            Order = 1
        });

        Assert.True(response.Id > 0);
        Assert.Equal(course.Id, response.CourseId);
        Assert.Equal("Basics", response.Title);
        Assert.Equal(1, response.Order);

        var saved = dbContext.Topics.FirstOrDefault(x => x.Id == response.Id);
        Assert.NotNull(saved);
        Assert.Equal(course.Id, saved!.CourseId);
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

        var course = new Course
        {
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic
        {
            CourseId = course.Id,
            Title = "Old",
            Order = 1
        };

        dbContext.Topics.Add(topic);

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        service.Update(topic.Id, new UpdateTopicRequest
        {
            Title = "New",
            Order = 2
        });

        var updated = dbContext.Topics.First(x => x.Id == topic.Id);

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

        var course = new Course
        {
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic
        {
            CourseId = course.Id,
            Title = "ToDelete",
            Order = 1
        };

        dbContext.Topics.Add(topic);

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        service.Delete(topic.Id);

        Assert.Empty(dbContext.Topics);
    }
    [Fact]
    public void Create_WhenOrderDuplicateInCourse_Throws()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course
        {
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic
        {
            CourseId = course.Id,
            Title = "T1",
            Order = 1
        };

        dbContext.Topics.Add(topic);

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        Assert.Throws<ArgumentException>(() => service.Create(new CreateTopicRequest
        {
            CourseId = course.Id,
            Title = "T2",
            Order = 1
        }));
    }

    [Fact]
    public void Create_WhenOrderIsNegative_NormalizesToZero()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminTopicService(dbContext);

        var course = new Course
        {
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var created = service.Create(new CreateTopicRequest
        {
            CourseId = course.Id,
            Title = "T",
            Order = -5
        });

        var topic = dbContext.Topics.First(x => x.Id == created.Id);

        Assert.Equal(0, topic.Order);
    }

    [Fact]
    public void Update_WhenOrderDuplicateInCourse_Throws()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course
        {
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic1 = new Topic
        {
            CourseId = course.Id,
            Title = "T1",
            Order = 1
        };

        var topic2 = new Topic
        {
            CourseId = course.Id,
            Title = "T2",
            Order = 2
        };

        dbContext.Topics.AddRange(topic1, topic2);

        dbContext.SaveChanges();

        var service = new AdminTopicService(dbContext);

        Assert.Throws<ArgumentException>(() => service.Update(topic2.Id, new UpdateTopicRequest
        {
            Title = "T2",
            Order = 1
        }));
    }
}
