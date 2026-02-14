﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class AdminSceneServiceTests
{
    [Fact]
    public void GetAll_ReturnsAllScenes()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Scenes.AddRange(
            new Scene { Id = 1, Title = "S1", Description = "D1", SceneType = "intro" },
            new Scene { Id = 2, Title = "S2", Description = "D2", SceneType = "dialog", BackgroundUrl = "bg", AudioUrl = "aud" }
        );

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        var result = service.GetAll();

        Assert.Equal(2, result.Count);

        Assert.Contains(result, x => x.Id == 1 && x.Title == "S1" && x.Description == "D1" && x.SceneType == "intro");
        Assert.Contains(result, x => x.Id == 2 && x.Title == "S2" && x.Description == "D2" && x.SceneType == "dialog"
                                     && x.BackgroundUrl == "bg" && x.AudioUrl == "aud");
    }

    [Fact]
    public void GetById_ReturnsScene_WithStepsOrderedByOrder()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedScene(dbContext, sceneId: 1);

        dbContext.SceneSteps.AddRange(
            new SceneStep { Id = 1, SceneId = 1, Order = 2, Speaker = "A", Text = "T2", StepType = "Text" },
            new SceneStep { Id = 2, SceneId = 1, Order = 1, Speaker = "B", Text = "T1", StepType = "Text" },
            new SceneStep { Id = 3, SceneId = 1, Order = 3, Speaker = "C", Text = "T3", StepType = "Text" }
        );

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        var result = service.GetById(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Scene 1", result.Title);
        Assert.Equal(3, result.Steps.Count);

        Assert.Equal(2, result.Steps[0].Id);
        Assert.Equal(1, result.Steps[1].Id);
        Assert.Equal(3, result.Steps[2].Id);

        Assert.Equal(1, result.Steps[0].Order);
        Assert.Equal(2, result.Steps[1].Order);
        Assert.Equal(3, result.Steps[2].Order);
    }

    [Fact]
    public void GetById_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminSceneService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetById(999));
    }

    [Fact]
    public void Create_AddsScene_WithSteps_AndReturnsDetails()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminSceneService(dbContext);

        var result = service.Create(new CreateSceneRequest
        {
            Title = "New Scene",
            Description = "Desc",
            SceneType = "intro",
            BackgroundUrl = "bg",
            AudioUrl = "aud",
            Steps = new()
            {
                new CreateSceneStepRequest { Order = 1, Speaker = "NPC", Text = "Hello", StepType = "Text" },
                new CreateSceneStepRequest { Order = 2, Speaker = "User", Text = "Hi", StepType = "Text", MediaUrl = "m1", ChoicesJson = "[\"A\",\"B\"]" }
            }
        });

        Assert.True(result.Id > 0);
        Assert.Equal("New Scene", result.Title);
        Assert.Equal(2, result.Steps.Count);

        var savedScene = dbContext.Scenes.FirstOrDefault(x => x.Id == result.Id);
        Assert.NotNull(savedScene);
        Assert.Equal("New Scene", savedScene!.Title);

        var savedSteps = dbContext.SceneSteps.Where(x => x.SceneId == result.Id).OrderBy(x => x.Order).ToList();
        Assert.Equal(2, savedSteps.Count);
        Assert.Equal(1, savedSteps[0].Order);
        Assert.Equal("NPC", savedSteps[0].Speaker);
        Assert.Equal(2, savedSteps[1].Order);
        Assert.Equal("User", savedSteps[1].Speaker);
    }

    [Fact]
    public void Create_NullRequest_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminSceneService(dbContext);

        Assert.Throws<ArgumentException>(() => service.Create(null!));
    }

    [Fact]
    public void Create_DuplicateStepOrders_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminSceneService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.Create(new CreateSceneRequest
            {
                Title = "S",
                Description = "D",
                SceneType = "intro",
                Steps = new()
                {
                    new CreateSceneStepRequest { Order = 1, Speaker = "A", Text = "T1", StepType = "Text" },
                    new CreateSceneStepRequest { Order = 1, Speaker = "B", Text = "T2", StepType = "Text" }
                }
            });
        });
    }

    [Fact]
    public void Update_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminSceneService(dbContext);

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.Update(999, new UpdateSceneRequest
            {
                Title = "T",
                Description = "D",
                SceneType = "intro",
                BackgroundUrl = "bg",
                AudioUrl = "aud"
            });
        });
    }

    [Fact]
    public void Update_UpdatesScene()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        var service = new AdminSceneService(dbContext);

        service.Update(1, new UpdateSceneRequest
        {
            Title = "Updated",
            Description = "Updated D",
            SceneType = "dialog",
            BackgroundUrl = "bg2",
            AudioUrl = "aud2"
        });

        var scene = dbContext.Scenes.FirstOrDefault(x => x.Id == 1);
        Assert.NotNull(scene);

        Assert.Equal("Updated", scene!.Title);
        Assert.Equal("Updated D", scene.Description);
        Assert.Equal("dialog", scene.SceneType);
        Assert.Equal("bg2", scene.BackgroundUrl);
        Assert.Equal("aud2", scene.AudioUrl);
    }

    [Fact]
    public void Delete_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminSceneService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.Delete(999));
    }

    [Fact]
    public void Delete_RemovesScene_Steps_AndAttempts()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedUser(dbContext, userId: 1);
        SeedUser(dbContext, userId: 2);
        SeedScene(dbContext, sceneId: 1);

        dbContext.SceneSteps.AddRange(
            new SceneStep { Id = 1, SceneId = 1, Order = 1, Speaker = "A", Text = "T1", StepType = "Text" },
            new SceneStep { Id = 2, SceneId = 1, Order = 2, Speaker = "B", Text = "T2", StepType = "Text" }
        );

        dbContext.SceneAttempts.AddRange(
            new SceneAttempt { Id = 1, UserId = 1, SceneId = 1, IsCompleted = true, CompletedAt = DateTime.UtcNow },
            new SceneAttempt { Id = 2, UserId = 2, SceneId = 1, IsCompleted = false, CompletedAt = DateTime.UtcNow }
        );

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        service.Delete(1);

        Assert.False(dbContext.Scenes.Any(x => x.Id == 1));
        Assert.False(dbContext.SceneSteps.Any(x => x.SceneId == 1));
        Assert.False(dbContext.SceneAttempts.Any(x => x.SceneId == 1));
    }

    [Fact]
    public void AddStep_WhenOrderAlreadyExists_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "T",
            StepType = "Text"
        });

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.AddStep(1, new CreateSceneStepRequest
            {
                Order = 1,
                Speaker = "B",
                Text = "T2",
                StepType = "Text"
            });
        });
    }

    [Fact]
    public void UpdateStep_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        var service = new AdminSceneService(dbContext);

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.UpdateStep(1, 999, new UpdateSceneStepRequest
            {
                Order = 1,
                Speaker = "A",
                Text = "T",
                StepType = "Text"
            });
        });
    }

    [Fact]
    public void DeleteStep_WhenNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        var service = new AdminSceneService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.DeleteStep(1, 999));
    }

    private static void SeedUser(Lumino.Api.Data.LuminoDbContext dbContext, int userId)
    {
        dbContext.Users.Add(new User
        {
            Id = userId,
            Email = $"user{userId}@test.com",
            PasswordHash = "hash",
            Role = Lumino.Api.Domain.Enums.Role.User,
            CreatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();
    }

    private static void SeedScene(Lumino.Api.Data.LuminoDbContext dbContext, int sceneId)
    {
        dbContext.Scenes.Add(new Scene
        {
            Id = sceneId,
            Title = $"Scene {sceneId}",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SaveChanges();
    }
}
