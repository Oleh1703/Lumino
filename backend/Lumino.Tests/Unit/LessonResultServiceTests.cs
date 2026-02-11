﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class LessonResultServiceTests
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
            new FakeSubmitLessonValidator(),
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
    }
}
