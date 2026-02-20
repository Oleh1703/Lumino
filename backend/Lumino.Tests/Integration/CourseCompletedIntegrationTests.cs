﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests.Integration;

public class CourseCompletedIntegrationTests
{
    [Fact]
    public void StartCourse_PassLastLesson_NextShouldBeNull_AndCompletionShouldBeCompleted()
    {
        var now = new DateTime(2026, 02, 16, 12, 0, 0, DateTimeKind.Utc);

        var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new FixedDateTimeProvider(now);

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Topic",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson 1",
            Theory = "",
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 1,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q1",
            Data = "",
            CorrectAnswer = "a",
            Order = 1
        });

        dbContext.SaveChanges();

        var settings = Options.Create<LearningSettings>(new LearningSettings
        {
            PassingScorePercent = 80,
            SceneUnlockEveryLessons = 1
        });

        var courseProgressService = new CourseProgressService(dbContext, dateTimeProvider, settings);

        var lessonResultService = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            dateTimeProvider,
            new SubmitLessonRequestValidator(),
            settings
        );

        var nextActivityService = new NextActivityService(dbContext, dateTimeProvider, settings);

        var courseCompletionService = new CourseCompletionService(dbContext, dateTimeProvider, settings);

        // 1) start course
        courseProgressService.StartCourse(10, 1);

        // 2) pass the last lesson
        var lessonSubmit = lessonResultService.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "a" }
            }
        });

        Assert.True(lessonSubmit.IsPassed);

        // 3) Next -> nothing left
        var next = nextActivityService.GetNext(10);

        Assert.NotNull(next);
        Assert.Equal("CourseComplete", next!.Type);
        Assert.Equal(1, next.CourseId);
        Assert.False(next.IsLocked);
        Assert.Null(next.LessonId);
        Assert.Null(next.SceneId);
        Assert.Null(next.UserVocabularyId);

        // 4) Completion endpoint logic -> Completed + persisted state
        var completion = courseCompletionService.GetMyCourseCompletion(10, 1);

        Assert.Equal(1, completion.CourseId);
        Assert.Equal("Completed", completion.Status);
        Assert.True(completion.IsCompleted);
        Assert.NotNull(completion.CompletedAt);

        Assert.Equal(1, completion.TotalLessons);
        Assert.Equal(1, completion.CompletedLessons);
        Assert.Equal(100, completion.CompletionPercent);

        Assert.Null(completion.NextLessonId);
        Assert.Empty(completion.RemainingLessonIds);

        // 5) state saved in db
        var userCourse = dbContext.UserCourses.FirstOrDefault(x => x.UserId == 10 && x.CourseId == 1);

        Assert.NotNull(userCourse);
        Assert.True(userCourse!.IsCompleted);
        Assert.False(userCourse.IsActive);
        Assert.Null(userCourse.LastLessonId);
        Assert.NotNull(userCourse.CompletedAt);
    }
}
