using Lumino.Api.Application.Services;
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

        var course = new Course
        {
            Title = "English A1",
            Description = "Demo",
            IsPublished = false

        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic
        {
            CourseId = course.Id,
            Title = "Basics",
            Order = 1

        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var service = new LessonService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetLessonsByTopic(topic.Id));
    }

    [Fact]
    public void GetLessonsByTopic_ReturnsLessonsOrderedByOrder()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course
        {
            Title = "English A1",
            Description = "Demo",
            IsPublished = true

        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic
        {
            CourseId = course.Id,
            Title = "Basics",
            Order = 1

        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var lesson1 = new Lesson { TopicId = topic.Id, Title = "L1", Theory = "T1", Order = 2 };
        var lesson2 = new Lesson { TopicId = topic.Id, Title = "L2", Theory = "T2", Order = 1 };
        var lesson3 = new Lesson { TopicId = topic.Id, Title = "L3", Theory = "T3", Order = 3 };

        dbContext.Lessons.AddRange(lesson1, lesson2, lesson3);
        dbContext.SaveChanges();

        var service = new LessonService(dbContext);

        var result = service.GetLessonsByTopic(topic.Id);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Order);
        Assert.Equal(2, result[1].Order);
        Assert.Equal(3, result[2].Order);
        Assert.Equal("L2", result[0].Title);
        Assert.Equal("L1", result[1].Title);
        Assert.Equal("L3", result[2].Title);
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

        var course = new Course
        {
            Title = "English A1",
            Description = "Demo",
            IsPublished = true
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic
        {
            CourseId = course.Id,
            Title = "Basics",
            Order = 1
        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var lesson1 = new Lesson
        {
            TopicId = topic.Id,
            Title = "Lesson 1",
            Theory = "Theory 1",
            Order = 1
        };

        var lesson2 = new Lesson
        {
            TopicId = topic.Id,
            Title = "Lesson 2",
            Theory = "Theory 2",
            Order = 2
        };

        dbContext.Lessons.AddRange(lesson1, lesson2);
        dbContext.SaveChanges();

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 10,
            LessonId = lesson2.Id,
            IsUnlocked = false,
            IsCompleted = false,
            BestScore = 0,
            LastAttemptAt = null
        });

        dbContext.SaveChanges();

        var service = new LessonService(dbContext);

        var ex = Assert.Throws<ForbiddenAccessException>(() => service.GetLessonById(10, lesson2.Id));

        Assert.Contains("Lesson is locked", ex.Message);
        Assert.Contains($"Complete lesson {lesson1.Id}", ex.Message);
    }


    [Fact]
    public void GetLessonById_WhenUnlocked_ReturnsLesson()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course
        {
            Title = "English A1",
            Description = "Demo",
            IsPublished = true
        };

        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic
        {
            CourseId = course.Id,
            Title = "Basics",
            Order = 1
        };

        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        var lesson = new Lesson
        {
            TopicId = topic.Id,
            Title = "Lesson 1",
            Theory = "Theory",
            Order = 1
        };

        dbContext.Lessons.Add(lesson);
        dbContext.SaveChanges();

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 10,
            LessonId = lesson.Id,
            IsUnlocked = true,
            IsCompleted = false,
            BestScore = 0,
            LastAttemptAt = null
        });

        dbContext.SaveChanges();

        var service = new LessonService(dbContext);

        var result = service.GetLessonById(10, lesson.Id);

        Assert.Equal(lesson.Id, result.Id);
        Assert.Equal(topic.Id, result.TopicId);
        Assert.Equal("Lesson 1", result.Title);
        Assert.Equal("Theory", result.Theory);
        Assert.Equal(1, result.Order);
    }
}
