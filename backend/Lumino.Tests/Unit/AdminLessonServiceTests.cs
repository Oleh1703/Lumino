using Lumino.Api.Application.DTOs;
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

        var topic = new Topic
        {
            CourseId = 1,
            Title = "Topic",
            Order = 1
        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var lesson1 = new Lesson { TopicId = topic.Id, Title = "L1", Theory = "T1", Order = 2 };
        var lesson2 = new Lesson { TopicId = topic.Id, Title = "L2", Theory = "T2", Order = 1 };
        var lesson3 = new Lesson { TopicId = topic.Id, Title = "L3", Theory = "T3", Order = 3 };

        dbContext.Lessons.AddRange(lesson1, lesson2, lesson3);

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        var result = service.GetByTopic(topic.Id);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Order);
        Assert.Equal(2, result[1].Order);
        Assert.Equal(3, result[2].Order);
        Assert.Equal("L2", result[0].Title);
        Assert.Equal("L1", result[1].Title);
        Assert.Equal("L3", result[2].Title);
    }

    [Fact]
    public void Create_AddsLesson_AndReturnsResponse()
    {
        var dbContext = TestDbContextFactory.Create();

        var topic = new Topic
        {
            CourseId = 1,
            Title = "Topic",
            Order = 1
        };

        dbContext.Topics.Add(topic);

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        var response = service.Create(new CreateLessonRequest
        {
            TopicId = topic.Id,
            Title = "Lesson",
            Theory = "Theory",
            Order = 1
        });

        Assert.True(response.Id > 0);
        Assert.Equal(topic.Id, response.TopicId);
        Assert.Equal("Lesson", response.Title);
        Assert.Equal("Theory", response.Theory);
        Assert.Equal(1, response.Order);

        var saved = dbContext.Lessons.FirstOrDefault(x => x.Id == response.Id);
        Assert.NotNull(saved);
        Assert.Equal(topic.Id, saved!.TopicId);
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

        var topic = new Topic
        {
            CourseId = 1,
            Title = "Topic",
            Order = 1
        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var lesson = new Lesson
        {
            TopicId = topic.Id,
            Title = "Old",
            Theory = "OldTheory",
            Order = 1
        };

        dbContext.Lessons.Add(lesson);

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        service.Update(lesson.Id, new UpdateLessonRequest
        {
            Title = "New",
            Theory = "NewTheory",
            Order = 2
        });

        var updated = dbContext.Lessons.First(x => x.Id == lesson.Id);

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

        var topic = new Topic
        {
            CourseId = 1,
            Title = "Topic",
            Order = 1
        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var lesson = new Lesson
        {
            TopicId = topic.Id,
            Title = "ToDelete",
            Theory = "T",
            Order = 1
        };

        dbContext.Lessons.Add(lesson);

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        service.Delete(lesson.Id);

        Assert.Empty(dbContext.Lessons);
    }
    [Fact]
    public void Create_WhenOrderDuplicateInTopic_Throws()
    {
        var dbContext = TestDbContextFactory.Create();

        var topic = new Topic
        {
            CourseId = 1,
            Title = "Topic",
            Order = 1
        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        dbContext.Lessons.Add(new Lesson
        {
            TopicId = topic.Id,
            Title = "L1",
            Theory = "T1",
            Order = 1
        });

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        Assert.Throws<ArgumentException>(() => service.Create(new CreateLessonRequest
        {
            TopicId = topic.Id,
            Title = "L2",
            Theory = "T2",
            Order = 1
        }));
    }

    [Fact]
    public void Create_WhenOrderIsNegative_NormalizesToZero()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminLessonService(dbContext);

        var topic = new Topic
        {
            CourseId = 1,
            Title = "Topic",
            Order = 1
        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var created = service.Create(new CreateLessonRequest
        {
            TopicId = topic.Id,
            Title = "L",
            Theory = "T",
            Order = -10
        });

        var lesson = dbContext.Lessons.First(x => x.Id == created.Id);

        Assert.Equal(0, lesson.Order);
    }

    [Fact]
    public void Update_WhenOrderDuplicateInTopic_Throws()
    {
        var dbContext = TestDbContextFactory.Create();

        var topic = new Topic
        {
            CourseId = 1,
            Title = "Topic",
            Order = 1
        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var lesson1 = new Lesson
        {
            TopicId = topic.Id,
            Title = "L1",
            Theory = "T1",
            Order = 1
        };

        var lesson2 = new Lesson
        {
            TopicId = topic.Id,
            Title = "L2",
            Theory = "T2",
            Order = 2
        };

        dbContext.Lessons.AddRange(lesson1, lesson2);

        dbContext.SaveChanges();

        var service = new AdminLessonService(dbContext);

        Assert.Throws<ArgumentException>(() => service.Update(lesson2.Id, new UpdateLessonRequest
        {
            Title = "L2",
            Theory = "T2",
            Order = 1
        }));
    }
}
