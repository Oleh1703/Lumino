﻿﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests.Integration;

public class LessonResultServiceIntegrationTests
{
    [Fact]
    public void SubmitLesson_ShouldCreateLessonResult_AndUpdateProgress()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            Title = "Lesson 1",
            Theory = "Text",
            TopicId = 1,
            Order = 1
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

        var service = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            new FakeDateTimeProvider(),
            new SubmitLessonRequestValidator(),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var response = service.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "hello" },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "world" }
            }
        });

        Assert.Equal(2, response.TotalExercises);
        Assert.Equal(2, response.CorrectAnswers);
        Assert.True(response.IsPassed);

        Assert.Equal(1, dbContext.LessonResults.Count(x => x.UserId == 10 && x.LessonId == 1));

        var progress = dbContext.UserProgresses.FirstOrDefault(x => x.UserId == 10);
        Assert.NotNull(progress);
        Assert.True(progress!.CompletedLessons >= 1);
        Assert.True(progress.TotalScore >= 2);
    }

    [Fact]
    public void SubmitLesson_EmptyAnswers_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        var service = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            new FakeDateTimeProvider(),
            new SubmitLessonRequestValidator(),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        Assert.Throws<ArgumentException>(() =>
        {
            service.SubmitLesson(10, new SubmitLessonRequest
            {
                LessonId = 1,
                Answers = new List<SubmitExerciseAnswerRequest>()
            });
        });
    }

    // lesson not found
    [Fact]
    public void SubmitLesson_LessonNotFound_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        var service = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            new FakeDateTimeProvider(),
            new SubmitLessonRequestValidator(),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.SubmitLesson(10, new SubmitLessonRequest
            {
                LessonId = 999,
                Answers = new List<SubmitExerciseAnswerRequest>
                {
                    new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "hello" }
                }
            });
        });
    }

    // partial correct -> IsPassed false
    [Fact]
    public void SubmitLesson_PartialCorrect_ShouldNotPass()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            Title = "Lesson 1",
            Theory = "Text",
            TopicId = 1,
            Order = 1
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

        var service = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            new FakeDateTimeProvider(),
            new SubmitLessonRequestValidator(),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var response = service.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "hello" },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "WRONG" }
            }
        });

        Assert.Equal(2, response.TotalExercises);
        Assert.Equal(1, response.CorrectAnswers);
        Assert.False(response.IsPassed);
    }

    // call twice -> progress accumulates
    [Fact]
    public void SubmitLesson_Twice_ShouldAccumulateUserProgress()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            Title = "Lesson 1",
            Theory = "Text",
            TopicId = 1,
            Order = 1
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

        dbContext.SaveChanges();

        var service = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            new FakeDateTimeProvider(),
            new SubmitLessonRequestValidator(),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var userId = 10;

        service.SubmitLesson(userId, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "hello" }
            }
        });

        service.SubmitLesson(userId, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "hello" }
            }
        });

        Assert.Equal(2, dbContext.LessonResults.Count(x => x.UserId == userId && x.LessonId == 1));

        var progress = dbContext.UserProgresses.FirstOrDefault(x => x.UserId == userId);
        Assert.NotNull(progress);

        Assert.Equal(1, progress!.CompletedLessons);
        Assert.Equal(2, progress.TotalScore);
    }
}
