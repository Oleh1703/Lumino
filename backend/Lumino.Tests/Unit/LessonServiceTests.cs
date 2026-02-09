using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
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
}
