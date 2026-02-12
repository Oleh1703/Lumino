using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class LessonResultQueryServiceTests
{
    [Fact]
    public void GetMyResults_ReturnsOnlyUserResults_OrderedByCompletedAtDesc()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.AddRange(
            new Lesson { Id = 1, TopicId = 1, Title = "Lesson 1", Theory = "T", Order = 1 },
            new Lesson { Id = 2, TopicId = 1, Title = "Lesson 2", Theory = "T", Order = 2 }
        );

        dbContext.LessonResults.AddRange(
            new LessonResult
            {
                Id = 1,
                UserId = 5,
                LessonId = 1,
                Score = 8,
                TotalQuestions = 10,
                MistakesJson = "[]",
                CompletedAt = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc)
            },
            new LessonResult
            {
                Id = 2,
                UserId = 5,
                LessonId = 2,
                Score = 7,
                TotalQuestions = 10,
                MistakesJson = "[]",
                CompletedAt = new DateTime(2026, 2, 11, 10, 0, 0, DateTimeKind.Utc)
            },

            // other user -> must not appear
            new LessonResult
            {
                Id = 3,
                UserId = 6,
                LessonId = 1,
                Score = 10,
                TotalQuestions = 10,
                MistakesJson = "[]",
                CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc)
            }
        );

        dbContext.SaveChanges();

        var service = new LessonResultQueryService(
            dbContext,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        var result = service.GetMyResults(5);

        Assert.Equal(2, result.Count);

        // newest first
        Assert.Equal(2, result[0].LessonId);
        Assert.Equal("Lesson 2", result[0].LessonTitle);

        Assert.Equal(1, result[1].LessonId);
        Assert.Equal("Lesson 1", result[1].LessonTitle);
    }

    [Fact]
    public void GetMyResultDetails_WhenNotFound_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        var service = new LessonResultQueryService(
            dbContext,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        var ex = Assert.Throws<KeyNotFoundException>(() =>
        {
            service.GetMyResultDetails(userId: 5, resultId: 999);
        });

        Assert.Equal("Lesson result not found", ex.Message);
    }

    [Fact]
    public void GetMyResultDetails_ArrayMistakesJson_ParsesMistakeIds_AndCalculatesIsPassed()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson 1",
            Theory = "T",
            Order = 1
        });

        var completedAt = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc);

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 5,
            LessonId = 1,
            Score = 8,
            TotalQuestions = 10,
            MistakesJson = "[1,2,3]",
            CompletedAt = completedAt
        });

        dbContext.SaveChanges();

        var service = new LessonResultQueryService(
            dbContext,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        var details = service.GetMyResultDetails(userId: 5, resultId: 1);

        Assert.Equal(1, details.ResultId);
        Assert.Equal(1, details.LessonId);
        Assert.Equal("Lesson 1", details.LessonTitle);

        Assert.Equal(8, details.Score);
        Assert.Equal(10, details.TotalQuestions);
        Assert.True(details.IsPassed);

        Assert.Equal(completedAt, details.CompletedAt);

        Assert.Equal(3, details.MistakeExerciseIds.Count);
        Assert.Equal(1, details.MistakeExerciseIds[0]);
        Assert.Equal(2, details.MistakeExerciseIds[1]);
        Assert.Equal(3, details.MistakeExerciseIds[2]);

        Assert.Empty(details.Answers);
    }

    [Fact]
    public void GetMyResultDetails_ObjectMistakesJson_ParsesMistakeIdsAndAnswers()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson 1",
            Theory = "T",
            Order = 1
        });

        var json = """
        {
          "mistakeExerciseIds": [5, 6],
          "answers": [
            { "exerciseId": 5, "userAnswer": "a", "correctAnswer": "b", "isCorrect": false },
            { "exerciseId": 6, "userAnswer": "c", "correctAnswer": "c", "isCorrect": true }
          ]
        }
        """;

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 5,
            LessonId = 1,
            Score = 7,
            TotalQuestions = 10,
            MistakesJson = json,
            CompletedAt = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var service = new LessonResultQueryService(
            dbContext,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        var details = service.GetMyResultDetails(userId: 5, resultId: 1);

        Assert.False(details.IsPassed);

        Assert.Equal(2, details.MistakeExerciseIds.Count);
        Assert.Equal(5, details.MistakeExerciseIds[0]);
        Assert.Equal(6, details.MistakeExerciseIds[1]);

        Assert.Equal(2, details.Answers.Count);

        Assert.Equal(5, details.Answers[0].ExerciseId);
        Assert.Equal("a", details.Answers[0].UserAnswer);
        Assert.Equal("b", details.Answers[0].CorrectAnswer);
        Assert.False(details.Answers[0].IsCorrect);

        Assert.Equal(6, details.Answers[1].ExerciseId);
        Assert.Equal("c", details.Answers[1].UserAnswer);
        Assert.Equal("c", details.Answers[1].CorrectAnswer);
        Assert.True(details.Answers[1].IsCorrect);
    }

    [Fact]
    public void GetMyResultDetails_InvalidJson_ReturnsEmptyDetails()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson 1",
            Theory = "T",
            Order = 1
        });

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 5,
            LessonId = 1,
            Score = 0,
            TotalQuestions = 0,
            MistakesJson = "{THIS IS NOT JSON",
            CompletedAt = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc)
        });

        dbContext.SaveChanges();

        var service = new LessonResultQueryService(
            dbContext,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneCompletionScore = 5 })
        );

        var details = service.GetMyResultDetails(userId: 5, resultId: 1);

        Assert.Empty(details.MistakeExerciseIds);
        Assert.Empty(details.Answers);

        // totalQuestions=0 -> IsPassed must be false by rules
        Assert.False(details.IsPassed);
    }
}
