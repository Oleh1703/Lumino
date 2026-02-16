﻿﻿﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests.Integration;

public class LearningHappyPathIntegrationTests
{
    [Fact]
    public void StartCourse_Then_Next_Then_SubmitLessonPassed_Then_Next_ShouldReturnNextLesson()
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

        dbContext.Lessons.Add(new Lesson
        {
            Id = 2,
            TopicId = 1,
            Title = "Lesson 2",
            Theory = "",
            Order = 2
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 1,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q1",
            Data = "",
            CorrectAnswer = "hello",
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 2,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q2",
            Data = "",
            CorrectAnswer = "world",
            Order = 2
        });

        dbContext.SaveChanges();

        var settings = Options.Create(new LearningSettings
        {
            PassingScorePercent = 80,
            SceneUnlockEveryLessons = 1
        });

        var courseProgressService = new CourseProgressService(dbContext, dateTimeProvider, settings);

        var nextActivityService = new NextActivityService(dbContext, dateTimeProvider, settings);

        var lessonResultService = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            dateTimeProvider,
            new SubmitLessonRequestValidator(),
            settings
        );

        // 1) start course -> should unlock first lesson
        var activeCourse = courseProgressService.StartCourse(10, 1);

        Assert.Equal(1, activeCourse.CourseId);
        Assert.NotNull(activeCourse.LastLessonId);

        // 2) next -> should return Lesson 1
        var next1 = nextActivityService.GetNext(10);

        Assert.NotNull(next1);
        Assert.Equal("Lesson", next1!.Type);
        Assert.Equal(1, next1.LessonId);

        // 3) submit lesson 1 passed
        var submit = lessonResultService.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "hello" },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "world" }
            }
        });

        Assert.True(submit.IsPassed);

        // 4) next -> should return Lesson 2 (unlocked after pass)
        var next2 = nextActivityService.GetNext(10);

        Assert.NotNull(next2);
        Assert.Equal("Lesson", next2!.Type);
        Assert.Equal(2, next2.LessonId);

        var progress1 = dbContext.UserLessonProgresses.FirstOrDefault(x => x.UserId == 10 && x.LessonId == 1);
        Assert.NotNull(progress1);
        Assert.True(progress1!.IsCompleted);

        var progress2 = dbContext.UserLessonProgresses.FirstOrDefault(x => x.UserId == 10 && x.LessonId == 2);
        Assert.NotNull(progress2);
        Assert.True(progress2!.IsUnlocked);
        Assert.False(progress2.IsCompleted);

        var userCourse = dbContext.UserCourses.FirstOrDefault(x => x.UserId == 10 && x.CourseId == 1 && x.IsActive);
        Assert.NotNull(userCourse);
        Assert.Equal(2, userCourse!.LastLessonId);
    }
    [Fact]
    public void StartCourse_Then_PassLessonWithMistake_Then_Next_ShouldReturnVocabularyReview_BeforeNextLesson()
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

        dbContext.Lessons.Add(new Lesson
        {
            Id = 2,
            TopicId = 1,
            Title = "Lesson 2",
            Theory = "",
            Order = 2
        });

        // 5 exercises -> 4 correct, 1 wrong -> 80% pass.
        // wrong exercise has pattern "Hello = ?" so it can be converted to a vocabulary pair (Hello, привіт).
        dbContext.Exercises.AddRange(
            new Exercise
            {
                Id = 1,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q1",
                Data = "",
                CorrectAnswer = "a",
                Order = 1
            },
            new Exercise
            {
                Id = 2,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q2",
                Data = "",
                CorrectAnswer = "b",
                Order = 2
            },
            new Exercise
            {
                Id = 3,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Hello = ?",
                Data = "",
                CorrectAnswer = "привіт",
                Order = 3
            },
            new Exercise
            {
                Id = 4,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q4",
                Data = "",
                CorrectAnswer = "d",
                Order = 4
            },
            new Exercise
            {
                Id = 5,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q5",
                Data = "",
                CorrectAnswer = "e",
                Order = 5
            }
        );

        dbContext.SaveChanges();

        var settings = Options.Create(new LearningSettings
        {
            PassingScorePercent = 80,
            SceneUnlockEveryLessons = 1
        });

        var courseProgressService = new CourseProgressService(dbContext, dateTimeProvider, settings);
        var nextActivityService = new NextActivityService(dbContext, dateTimeProvider, settings);

        var lessonResultService = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            dateTimeProvider,
            new SubmitLessonRequestValidator(),
            settings
        );

        var vocabularyService = new VocabularyService(dbContext, dateTimeProvider, settings);

        // 1) start course
        courseProgressService.StartCourse(10, 1);

        // 2) submit lesson 1 (passed but with 1 mistake)
        var submit = lessonResultService.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "a" },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "b" },
                new SubmitExerciseAnswerRequest { ExerciseId = 3, Answer = "WRONG" }, // mistake -> vocab due now
                new SubmitExerciseAnswerRequest { ExerciseId = 4, Answer = "d" },
                new SubmitExerciseAnswerRequest { ExerciseId = 5, Answer = "e" }
            }
        });

        Assert.True(submit.IsPassed);
        Assert.Contains(3, submit.MistakeExerciseIds);

        // lesson 2 should be unlocked, but NextActivity must return VocabularyReview first.
        var next1 = nextActivityService.GetNext(10);

        Assert.NotNull(next1);
        Assert.Equal("VocabularyReview", next1!.Type);
        Assert.Equal("hello", next1.Word);
        Assert.Equal("привіт", next1.Translation);

        // 3) review the due word and then NextActivity should return lesson 2
        var due = vocabularyService.GetNextReview(10);

        Assert.NotNull(due);
        Assert.Equal("hello", due!.Word);
        Assert.Equal("привіт", due.Translation);

        vocabularyService.ReviewWord(10, due.Id, new ReviewVocabularyRequest { IsCorrect = true });

        var next2 = nextActivityService.GetNext(10);

        Assert.NotNull(next2);
        Assert.Equal("Lesson", next2!.Type);
        Assert.Equal(2, next2.LessonId);
    }

}
