﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests.Integration;

public class SceneMistakesFlowIntegrationTests
{
    [Fact]
    public void SubmitScene_WithMistake_GetSceneMistakes_SubmitSceneMistakes_ShouldCompleteAndClearMistakes()
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

        // Scene position = 1 -> requires 0 passed lessons => always unlocked.
        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            CourseId = 1,
            Order = 1,
            Title = "Scene 1",
            Description = "",
            SceneType = "Quiz",
            BackgroundUrl = null,
            AudioUrl = null
        });

        // 2 question steps (ChoicesJson not empty) => totalQuestions = 2
        dbContext.SceneSteps.AddRange(
            new SceneStep
            {
                Id = 1,
                SceneId = 1,
                Order = 1,
                Speaker = "NPC",
                Text = "Pick A",
                StepType = "Choice",
                MediaUrl = null,
                ChoicesJson = "[{\"text\":\"A\",\"isCorrect\":true},{\"text\":\"B\",\"isCorrect\":false}]"
            },
            new SceneStep
            {
                Id = 2,
                SceneId = 1,
                Order = 2,
                Speaker = "NPC",
                Text = "Pick C",
                StepType = "Choice",
                MediaUrl = null,
                ChoicesJson = "[{\"text\":\"C\",\"isCorrect\":true},{\"text\":\"D\",\"isCorrect\":false}]"
            }
        );

        dbContext.SaveChanges();

        var settings = Options.Create<LearningSettings>(new LearningSettings
        {
            PassingScorePercent = 80,
            SceneUnlockEveryLessons = 1
        });

        var sceneService = new SceneService(
            dbContext,
            dateTimeProvider,
            new FakeAchievementService(),
            settings
        );

        // 1) SubmitScene with one mistake (step 1 wrong), step 2 correct
        var submit1 = sceneService.SubmitScene(10, 1, new SubmitSceneRequest
        {
            Answers = new List<SubmitSceneAnswerRequest>
            {
                new SubmitSceneAnswerRequest { StepId = 1, Answer = "B" }, // wrong
                new SubmitSceneAnswerRequest { StepId = 2, Answer = "C" }  // correct
            }
        });

        Assert.Equal(1, submit1.SceneId);
        Assert.Equal(2, submit1.TotalQuestions);
        Assert.Equal(1, submit1.CorrectAnswers);
        Assert.False(submit1.IsCompleted);
        Assert.Single(submit1.MistakeStepIds);
        Assert.Contains(1, submit1.MistakeStepIds);

        // 2) GetSceneMistakes should return the wrong step
        var mistakes = sceneService.GetSceneMistakes(10, 1);

        Assert.Equal(1, mistakes.SceneId);
        Assert.Equal(1, mistakes.TotalMistakes);
        Assert.Single(mistakes.MistakeStepIds);
        Assert.Contains(1, mistakes.MistakeStepIds);
        Assert.Single(mistakes.Steps);
        Assert.Equal(1, mistakes.Steps[0].Id);

        // 3) SubmitSceneMistakes with corrected answer only for mistake step
        var submit2 = sceneService.SubmitSceneMistakes(10, 1, new SubmitSceneRequest
        {
            Answers = new List<SubmitSceneAnswerRequest>
            {
                new SubmitSceneAnswerRequest { StepId = 1, Answer = "A" } // fix
            }
        });

        Assert.Equal(1, submit2.SceneId);
        Assert.Equal(2, submit2.TotalQuestions);
        Assert.Equal(2, submit2.CorrectAnswers);
        Assert.True(submit2.IsCompleted);
        Assert.Empty(submit2.MistakeStepIds);

        // 4) GetSceneMistakes after fix should be empty
        var mistakesAfter = sceneService.GetSceneMistakes(10, 1);

        Assert.Equal(1, mistakesAfter.SceneId);
        Assert.Equal(0, mistakesAfter.TotalMistakes);
        Assert.Empty(mistakesAfter.MistakeStepIds);
        Assert.Empty(mistakesAfter.Steps);
    }
}
