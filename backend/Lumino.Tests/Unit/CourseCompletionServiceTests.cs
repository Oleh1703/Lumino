using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class CourseCompletionServiceTests
{
    [Fact]
    public void GetMyCourseCompletion_WhenCourseNotFound_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);

        var service = new CourseCompletionService(
            dbContext,
            new FixedDateTimeProvider(now),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.GetMyCourseCompletion(userId: 1, courseId: 999);
        });
    }

    [Fact]
    public void GetMyCourseCompletion_WhenNotStarted_ShouldReturnNotStarted()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course { Title = "English A1", Description = "Demo", IsPublished = true };
        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic { CourseId = course.Id, Title = "Basics", Order = 1 };
        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = topic.Id, Title = "L1", Theory = "T", Order = 1 },
            new Lesson { Id = 2, TopicId = topic.Id, Title = "L2", Theory = "T", Order = 2 },
            new Lesson { Id = 3, TopicId = topic.Id, Title = "L3", Theory = "T", Order = 3 }
        );

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);

        var service = new CourseCompletionService(
            dbContext,
            new FixedDateTimeProvider(now),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        var result = service.GetMyCourseCompletion(userId: 10, courseId: course.Id);

        Assert.Equal(course.Id, result.CourseId);
        Assert.Equal("NotStarted", result.Status);

        Assert.False(result.IsCompleted);
        Assert.Null(result.CompletedAt);

        Assert.Equal(3, result.TotalLessons);
        Assert.Equal(0, result.CompletedLessons);
        Assert.Equal(0, result.CompletionPercent);
        Assert.Equal(1, result.NextLessonId);
        Assert.Equal(3, result.RemainingLessonIds.Count);

        Assert.False(result.ScenesIncluded);
        Assert.Equal(0, result.ScenesTotal);
        Assert.Equal(0, result.ScenesCompleted);
        Assert.Equal(0, result.ScenesCompletionPercent);
    }

    [Fact]
    public void GetMyCourseCompletion_WhenInProgress_ShouldReturnInProgress()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course { Title = "English A1", Description = "Demo", IsPublished = true };
        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic { CourseId = course.Id, Title = "Basics", Order = 1 };
        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = topic.Id, Title = "L1", Theory = "T", Order = 1 },
            new Lesson { Id = 2, TopicId = topic.Id, Title = "L2", Theory = "T", Order = 2 },
            new Lesson { Id = 3, TopicId = topic.Id, Title = "L3", Theory = "T", Order = 3 },
            new Lesson { Id = 4, TopicId = topic.Id, Title = "L4", Theory = "T", Order = 4 }
        );

        var userId = 10;

        // passed only lesson 1 (80%+)
        dbContext.LessonResults.Add(new LessonResult
        {
            UserId = userId,
            LessonId = 1,
            Score = 8,
            TotalQuestions = 10,
            CompletedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc)
        });

        // user started course (to mark not NotStarted)
        dbContext.UserCourses.Add(new UserCourse
        {
            UserId = userId,
            CourseId = course.Id,
            IsActive = true,
            StartedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc),
            LastLessonId = 2,
            LastOpenedAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);

        var service = new CourseCompletionService(
            dbContext,
            new FixedDateTimeProvider(now),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        var result = service.GetMyCourseCompletion(userId: userId, courseId: course.Id);

        Assert.Equal("InProgress", result.Status);

        Assert.False(result.IsCompleted);
        Assert.Null(result.CompletedAt);

        Assert.Equal(4, result.TotalLessons);
        Assert.Equal(1, result.CompletedLessons);
        Assert.Equal(25, result.CompletionPercent);
        Assert.Equal(2, result.NextLessonId);
        Assert.Equal(new List<int> { 2, 3, 4 }, result.RemainingLessonIds);
    }

    [Fact]
    public void GetMyCourseCompletion_WhenCompleted_ShouldReturnCompleted_AndPersistState()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course { Title = "English A1", Description = "Demo", IsPublished = true };
        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic { CourseId = course.Id, Title = "Basics", Order = 1 };
        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = topic.Id, Title = "L1", Theory = "T", Order = 1 },
            new Lesson { Id = 2, TopicId = topic.Id, Title = "L2", Theory = "T", Order = 2 },
            new Lesson { Id = 3, TopicId = topic.Id, Title = "L3", Theory = "T", Order = 3 }
        );

        var userId = 10;

        // passed all 3 lessons
        dbContext.LessonResults.AddRange(
            new LessonResult { UserId = userId, LessonId = 1, Score = 10, TotalQuestions = 10, CompletedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 2, Score = 8, TotalQuestions = 10, CompletedAt = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 3, Score = 9, TotalQuestions = 10, CompletedAt = new DateTime(2026, 2, 3, 10, 0, 0, DateTimeKind.Utc) }
        );

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);

        var service = new CourseCompletionService(
            dbContext,
            new FixedDateTimeProvider(now),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        var result = service.GetMyCourseCompletion(userId: userId, courseId: course.Id);

        Assert.Equal("Completed", result.Status);

        Assert.True(result.IsCompleted);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal(now, result.CompletedAt);

        Assert.Equal(3, result.TotalLessons);
        Assert.Equal(3, result.CompletedLessons);
        Assert.Equal(100, result.CompletionPercent);
        Assert.Null(result.NextLessonId);
        Assert.Empty(result.RemainingLessonIds);

        var userCourse = dbContext.UserCourses.FirstOrDefault(x => x.UserId == userId && x.CourseId == course.Id);
        Assert.NotNull(userCourse);
        Assert.True(userCourse!.IsCompleted);
        Assert.Equal(now, userCourse.CompletedAt);
    }


    [Fact]
    public void GetMyCourseCompletion_WhenLessonsCompletedButScenesNotCompleted_ShouldReturnInProgress()
    {
        var dbContext = TestDbContextFactory.Create();

        var course = new Course { Title = "English A1", Description = "Demo", IsPublished = true };
        dbContext.Courses.Add(course);
        dbContext.SaveChanges();

        var topic = new Topic { CourseId = course.Id, Title = "Basics", Order = 1 };
        dbContext.Topics.Add(topic);
        dbContext.SaveChanges();

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = topic.Id, Title = "L1", Theory = "T", Order = 1 },
            new Lesson { Id = 2, TopicId = topic.Id, Title = "L2", Theory = "T", Order = 2 }
        );

        var userId = 10;

        dbContext.LessonResults.AddRange(
            new LessonResult { UserId = userId, LessonId = 1, Score = 8, TotalQuestions = 10, CompletedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 2, Score = 9, TotalQuestions = 10, CompletedAt = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc) }
        );

        dbContext.Scenes.AddRange(
            new Scene { CourseId = course.Id, Title = "S1", Description = "D", SceneType = "Dialog", Order = 1 },
            new Scene { CourseId = course.Id, Title = "S2", Description = "D", SceneType = "Dialog", Order = 2 }
        );

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);

        var service = new CourseCompletionService(
            dbContext,
            new FixedDateTimeProvider(now),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5, SceneUnlockEveryLessons = 1 })
        );

        var result = service.GetMyCourseCompletion(userId: userId, courseId: course.Id);

        Assert.Equal("InProgress", result.Status);

        Assert.False(result.IsCompleted);
        Assert.Null(result.CompletedAt);

        Assert.True(result.ScenesIncluded);
        Assert.Equal(2, result.ScenesTotal);
        Assert.Equal(0, result.ScenesCompleted);
        Assert.Equal(0, result.ScenesCompletionPercent);
    }

    [Fact]
    public void GetMyCourseCompletion_WhenHasScenesInOtherCourse_ShouldCountOnlyCourseScenes()
    {
        var dbContext = TestDbContextFactory.Create();

        var courseA = new Course { Title = "English A1", Description = "A", IsPublished = true };
        var courseB = new Course { Title = "English A2", Description = "B", IsPublished = true };
        dbContext.Courses.AddRange(courseA, courseB);
        dbContext.SaveChanges();

        var topicA = new Topic { CourseId = courseA.Id, Title = "Basics", Order = 1 };
        dbContext.Topics.Add(topicA);
        dbContext.SaveChanges();

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = topicA.Id, Title = "L1", Theory = "T", Order = 1 },
            new Lesson { Id = 2, TopicId = topicA.Id, Title = "L2", Theory = "T", Order = 2 }
        );

        var userId = 10;

        dbContext.LessonResults.AddRange(
            new LessonResult { UserId = userId, LessonId = 1, Score = 9, TotalQuestions = 10, CompletedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc) },
            new LessonResult { UserId = userId, LessonId = 2, Score = 8, TotalQuestions = 10, CompletedAt = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc) }
        );

        // course A scenes (Order controls unlock order)
        dbContext.Scenes.AddRange(
            new Scene { Id = 100, CourseId = courseA.Id, Title = "A-1", Description = "D", SceneType = "Dialog", Order = 1 },
            new Scene { Id = 101, CourseId = courseA.Id, Title = "A-2", Description = "D", SceneType = "Dialog", Order = 2 }
        );

        // course B scenes must NOT be counted for course A completion
        dbContext.Scenes.AddRange(
            new Scene { Id = 200, CourseId = courseB.Id, Title = "B-1", Description = "D", SceneType = "Dialog", Order = 1 },
            new Scene { Id = 201, CourseId = courseB.Id, Title = "B-2", Description = "D", SceneType = "Dialog", Order = 2 },
            new Scene { Id = 202, CourseId = courseB.Id, Title = "B-3", Description = "D", SceneType = "Dialog", Order = 3 }
        );

        dbContext.SaveChanges();

        // complete only first course A scene
        dbContext.SceneAttempts.Add(new SceneAttempt
        {
            UserId = userId,
            SceneId = 100,
            IsCompleted = true,
            Score = 5,
            TotalQuestions = 0,
            CompletedAt = new DateTime(2026, 2, 3, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);

        var service = new CourseCompletionService(
            dbContext,
            new FixedDateTimeProvider(now),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5, SceneUnlockEveryLessons = 1 })
        );

        var result = service.GetMyCourseCompletion(userId: userId, courseId: courseA.Id);

        Assert.Equal(courseA.Id, result.CourseId);
        Assert.Equal("InProgress", result.Status);

        Assert.True(result.ScenesIncluded);
        Assert.Equal(2, result.ScenesTotal);
        Assert.Equal(1, result.ScenesCompleted);
        Assert.Equal(50, result.ScenesCompletionPercent);
    }

}
