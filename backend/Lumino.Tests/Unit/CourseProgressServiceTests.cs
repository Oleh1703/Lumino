using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Tests;
using Xunit;

namespace Lumino.Tests;

public class CourseProgressServiceTests
{
    [Fact]
    public void StartCourse_WhenCourseNotFound_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var service = new CourseProgressService(dbContext, dateTimeProvider);

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.StartCourse(userId: 1, courseId: 999);
        });
    }

    [Fact]
    public void StartCourse_ShouldSetActive_AndDeactivateOthers_AndUnlockFirstLesson()
    {
        var dbContext = TestDbContextFactory.Create();

        var userId = 1;

        var course1 = new Course { Title = "Course 1", Description = "Desc 1", IsPublished = true };
        var course2 = new Course { Title = "Course 2", Description = "Desc 2", IsPublished = true };

        dbContext.Courses.Add(course1);
        dbContext.Courses.Add(course2);
        dbContext.SaveChanges();

        var t1 = new Topic { CourseId = course1.Id, Title = "T1", Order = 1 };
        var t2 = new Topic { CourseId = course2.Id, Title = "T2", Order = 1 };

        dbContext.Topics.Add(t1);
        dbContext.Topics.Add(t2);
        dbContext.SaveChanges();

        var c1_l1 = new Lesson { TopicId = t1.Id, Title = "C1L1", Theory = "Theory", Order = 1 };
        var c1_l2 = new Lesson { TopicId = t1.Id, Title = "C1L2", Theory = "Theory", Order = 2 };

        var c2_l1 = new Lesson { TopicId = t2.Id, Title = "C2L1", Theory = "Theory", Order = 1 };

        dbContext.Lessons.Add(c1_l1);
        dbContext.Lessons.Add(c1_l2);
        dbContext.Lessons.Add(c2_l1);
        dbContext.SaveChanges();

        var oldNow = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc);
        dbContext.UserCourses.Add(new UserCourse
        {
            UserId = userId,
            CourseId = course2.Id,
            IsActive = true,
            StartedAt = oldNow,
            LastLessonId = c2_l1.Id,
            LastOpenedAt = oldNow
        });

        // Створимо прогрес для першого уроку, але заблокуємо його
        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = userId,
            LessonId = c1_l1.Id,
            IsUnlocked = false,
            IsCompleted = false,
            BestScore = 0,
            LastAttemptAt = null
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var service = new CourseProgressService(dbContext, dateTimeProvider);

        var response = service.StartCourse(userId, course1.Id);

        Assert.Equal(course1.Id, response.CourseId);
        Assert.Equal(now, response.LastOpenedAt);
        Assert.Equal(c1_l1.Id, response.LastLessonId);

        var activeCourse1 = dbContext.UserCourses.First(x => x.UserId == userId && x.CourseId == course1.Id);
        Assert.True(activeCourse1.IsActive);
        Assert.Equal(now, activeCourse1.LastOpenedAt);
        Assert.Equal(c1_l1.Id, activeCourse1.LastLessonId);

        var deactivatedCourse2 = dbContext.UserCourses.First(x => x.UserId == userId && x.CourseId == course2.Id);
        Assert.False(deactivatedCourse2.IsActive);
        Assert.Equal(now, deactivatedCourse2.LastOpenedAt);

        var progress = dbContext.UserLessonProgresses.First(x => x.UserId == userId && x.LessonId == c1_l1.Id);
        Assert.True(progress.IsUnlocked);
        Assert.Equal(now, progress.LastAttemptAt);
    }

    [Fact]
    public void GetMyActiveCourse_WhenNone_ReturnsNull()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var service = new CourseProgressService(dbContext, dateTimeProvider);

        var result = service.GetMyActiveCourse(userId: 1);

        Assert.Null(result);
    }

    [Fact]
    public void GetMyLessonProgressByCourse_ShouldReturnLessonsInOrder_WithDefaults()
    {
        var dbContext = TestDbContextFactory.Create();

        var userId = 1;

        var course = new Course { Title = "Course", Description = "Desc", IsPublished = true };
        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic1 = new Topic { CourseId = course.Id, Title = "T1", Order = 1 };
        var topic2 = new Topic { CourseId = course.Id, Title = "T2", Order = 2 };

        dbContext.Topics.Add(topic1);
        dbContext.Topics.Add(topic2);
        dbContext.SaveChanges();

        var l1 = new Lesson { TopicId = topic1.Id, Title = "L1", Theory = "Theory", Order = 1 };
        var l2 = new Lesson { TopicId = topic1.Id, Title = "L2", Theory = "Theory", Order = 2 };
        var l3 = new Lesson { TopicId = topic2.Id, Title = "L3", Theory = "Theory", Order = 1 };

        dbContext.Lessons.Add(l1);
        dbContext.Lessons.Add(l2);
        dbContext.Lessons.Add(l3);
        dbContext.SaveChanges();

        // Є прогрес лише на L2
        var attemptAt = new DateTime(2026, 2, 11, 8, 0, 0, DateTimeKind.Utc);

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = userId,
            LessonId = l2.Id,
            IsUnlocked = true,
            IsCompleted = true,
            BestScore = 3,
            LastAttemptAt = attemptAt
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);
        var service = new CourseProgressService(dbContext, dateTimeProvider);

        var list = service.GetMyLessonProgressByCourse(userId, course.Id);

        Assert.Equal(3, list.Count);

        // порядок: topic1 (order=1) lessons 1,2 -> потім topic2 lesson
        Assert.Equal(l1.Id, list[0].LessonId);
        Assert.Equal(l2.Id, list[1].LessonId);
        Assert.Equal(l3.Id, list[2].LessonId);

        // L1 default
        Assert.False(list[0].IsUnlocked);
        Assert.False(list[0].IsCompleted);
        Assert.Equal(0, list[0].BestScore);
        Assert.Null(list[0].LastAttemptAt);

        // L2 existing
        Assert.True(list[1].IsUnlocked);
        Assert.True(list[1].IsCompleted);
        Assert.Equal(3, list[1].BestScore);
        Assert.Equal(attemptAt, list[1].LastAttemptAt);

        // L3 default
        Assert.False(list[2].IsUnlocked);
        Assert.False(list[2].IsCompleted);
        Assert.Equal(0, list[2].BestScore);
        Assert.Null(list[2].LastAttemptAt);
    }
}
