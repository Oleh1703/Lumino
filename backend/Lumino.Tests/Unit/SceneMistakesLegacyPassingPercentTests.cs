using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests;

public class SceneMistakesLegacyPassingPercentTests
{
    [Fact]
    public void SubmitSceneMistakes_WhenMistakeStepIdsAreNotQuestionSteps_ShouldUseScenePassingPercent()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 10,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            CourseId = 1,
            Order = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "Dialog",
            BackgroundUrl = null,
            AudioUrl = null
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Q1",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"A\",\"isCorrect\":true},{\"text\":\"B\",\"isCorrect\":false}]"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 2,
            SceneId = 1,
            Order = 2,
            Speaker = "A",
            Text = "Q2",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"A\",\"isCorrect\":true},{\"text\":\"B\",\"isCorrect\":false}]"
        });

        var legacyDetails = new SceneAttemptDetailsJson
        {
            MistakeStepIds = new List<int> { 999 }, // не існує серед question steps
            Answers = new List<SceneStepAnswerResultDto>
            {
                new SceneStepAnswerResultDto { StepId = 1, UserAnswer = "A", CorrectAnswer = "A", IsCorrect = true },
                new SceneStepAnswerResultDto { StepId = 2, UserAnswer = "B", CorrectAnswer = "A", IsCorrect = false }
            }
        };

        var legacyJson = JsonSerializer.Serialize(legacyDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        dbContext.SceneAttempts.Add(new SceneAttempt
        {
            UserId = 10,
            SceneId = 1,
            IsCompleted = false,
            CompletedAt = DateTime.UtcNow.AddDays(-1),
            Score = 1,
            TotalQuestions = 2,
            DetailsJson = legacyJson
        });

        dbContext.SaveChanges();

        var learning = Options.Create(new LearningSettings
        {
            ScenePassingPercent = 50,
            SceneCompletionScore = 5,
            PassingScorePercent = 80,
            DailyGoalScoreTarget = 20
        });

        var service = new SceneService(
            dbContext,
            new FixedDateTimeProvider(DateTime.UtcNow),
            new FakeAchievementService(),
            learning
        );

        var req = new SubmitSceneRequest
        {
            IdempotencyKey = "legacy-1",
            Answers = new List<SubmitSceneAnswerRequest>() // без нових відповідей
        };

        var result = service.SubmitSceneMistakes(10, 1, req);

        Assert.True(result.IsCompleted);
        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);

        var attempt = dbContext.SceneAttempts.First(x => x.UserId == 10 && x.SceneId == 1);
        Assert.True(attempt.IsCompleted);
    }
}
