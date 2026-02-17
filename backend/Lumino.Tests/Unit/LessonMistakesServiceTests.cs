using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests;

public class LessonMistakesServiceTests
{
    [Fact]
    public void GetLessonMistakes_ShouldReturnMistakeExercises_FromLastResult()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson 1",
            Theory = "Theory"
        });

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 1,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 10,
            LessonId = 1,
            Order = 1,
            Type = ExerciseType.Input,
            Question = "Translate",
            CorrectAnswer = "hello",
            Data = "{}"
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 11,
            LessonId = 1,
            Order = 2,
            Type = ExerciseType.Input,
            Question = "Translate",
            CorrectAnswer = "world",
            Data = "{}"
        });

        var details = new LessonResultDetailsJson
        {
            MistakeExerciseIds = new() { 11 },
            Answers = new()
            {
                new LessonAnswerResultDto { ExerciseId = 10, UserAnswer = "hello", CorrectAnswer = "hello", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 11, UserAnswer = "", CorrectAnswer = "world", IsCorrect = false }
            }
        };

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 1,
            Score = 1,
            TotalQuestions = 2,
            CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
            MistakesJson = JsonSerializer.Serialize(details)
        });

        dbContext.SaveChanges();

        var service = new LessonMistakesService(dbContext);

        var result = service.GetLessonMistakes(userId: 1, lessonId: 1);

        Assert.Equal(1, result.LessonId);
        Assert.Equal(1, result.TotalMistakes);
        Assert.Single(result.MistakeExerciseIds);
        Assert.Equal(11, result.MistakeExerciseIds[0]);

        Assert.Single(result.Exercises);
        Assert.Equal(11, result.Exercises[0].Id);
    }

    [Fact]
    public void SubmitLessonMistakes_WhenAnswerCorrect_ShouldClearMistakes_AndUpdateResult()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson 1",
            Theory = "Theory"
        });

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 1,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 10,
            LessonId = 1,
            Order = 1,
            Type = ExerciseType.Input,
            Question = "Translate",
            CorrectAnswer = "hello",
            Data = "{}"
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 11,
            LessonId = 1,
            Order = 2,
            Type = ExerciseType.Input,
            Question = "Translate",
            CorrectAnswer = "world",
            Data = "{}"
        });

        var details = new LessonResultDetailsJson
        {
            MistakeExerciseIds = new() { 11 },
            Answers = new()
            {
                new LessonAnswerResultDto { ExerciseId = 10, UserAnswer = "hello", CorrectAnswer = "hello", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 11, UserAnswer = "WRONG", CorrectAnswer = "world", IsCorrect = false }
            }
        };

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 1,
            Score = 1,
            TotalQuestions = 2,
            CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
            MistakesJson = JsonSerializer.Serialize(details)
        });

        dbContext.SaveChanges();

        var service = new LessonMistakesService(dbContext);

        var response = service.SubmitLessonMistakes(userId: 1, lessonId: 1, new SubmitLessonMistakesRequest
        {
            Answers = new()
            {
                new SubmitExerciseAnswerRequest
                {
                    ExerciseId = 11,
                    Answer = "world"
                }
            }
        });

        Assert.Equal(1, response.LessonId);
        Assert.True(response.IsCompleted);
        Assert.Empty(response.MistakeExerciseIds);
        Assert.Equal(2, response.TotalExercises);
        Assert.Equal(2, response.CorrectAnswers);

        var saved = dbContext.LessonResults.First(x => x.Id == 1);

        Assert.Equal(2, saved.Score);
        Assert.Equal(2, saved.TotalQuestions);
        Assert.False(string.IsNullOrWhiteSpace(saved.MistakesJson));

        var updated = JsonSerializer.Deserialize<LessonResultDetailsJson>(saved.MistakesJson!, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(updated);
        Assert.Empty(updated!.MistakeExerciseIds);
        Assert.Equal(2, updated.Answers.Count);
        Assert.True(updated.Answers.First(x => x.ExerciseId == 11).IsCorrect);
    }
}
