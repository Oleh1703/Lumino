using Lumino.Api.Application.DTOs;
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
    public void GetById_ReturnsScene_WithStepsOrderedByOrder_AndZeroGoesLast()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedScene(dbContext, sceneId: 1);

        dbContext.SceneSteps.AddRange(
            new SceneStep { Id = 10, SceneId = 1, Order = 2, Speaker = "A", Text = "T2", StepType = "Text" },
            new SceneStep { Id = 11, SceneId = 1, Order = 0, Speaker = "B", Text = "T0", StepType = "Text" },
            new SceneStep { Id = 12, SceneId = 1, Order = 1, Speaker = "C", Text = "T1", StepType = "Text" },
            new SceneStep { Id = 13, SceneId = 1, Order = -1, Speaker = "D", Text = "T-1", StepType = "Text" }
        );

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        var result = service.GetById(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Scene 1", result.Title);
        Assert.Equal(4, result.Steps.Count);

        Assert.Equal(12, result.Steps[0].Id);
        Assert.Equal(10, result.Steps[1].Id);
        Assert.Equal(11, result.Steps[2].Id);
        Assert.Equal(13, result.Steps[3].Id);

        Assert.Equal(1, result.Steps[0].Order);
        Assert.Equal(2, result.Steps[1].Order);
        Assert.Equal(0, result.Steps[2].Order);
        Assert.Equal(-1, result.Steps[3].Order);
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
    public void Create_DuplicatePositiveStepOrders_Throws()
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
    public void Create_DuplicateZeroStepOrders_ShouldNotThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminSceneService(dbContext);

        var result = service.Create(new CreateSceneRequest
        {
            Title = "S",
            Description = "D",
            SceneType = "intro",
            Steps = new()
            {
                new CreateSceneStepRequest { Order = 0, Speaker = "A", Text = "T1", StepType = "Text" },
                new CreateSceneStepRequest { Order = 0, Speaker = "B", Text = "T2", StepType = "Text" }
            }
        });

        Assert.True(result.Id > 0);
        Assert.Equal(2, result.Steps.Count);
        Assert.All(result.Steps, x => Assert.Equal(0, x.Order));
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
    public void GetSteps_WhenSceneNotFound_Throws()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new AdminSceneService(dbContext);

        Assert.Throws<KeyNotFoundException>(() => service.GetSteps(999));
    }

    [Fact]
    public void GetSteps_ReturnsOrderedByOrder_AndZeroGoesLast()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        dbContext.SceneSteps.AddRange(
            new SceneStep { Id = 1, SceneId = 1, Order = 3, Speaker = "A", Text = "T3", StepType = "Text" },
            new SceneStep { Id = 2, SceneId = 1, Order = 0, Speaker = "B", Text = "T0", StepType = "Text" },
            new SceneStep { Id = 3, SceneId = 1, Order = 2, Speaker = "C", Text = "T2", StepType = "Text" },
            new SceneStep { Id = 4, SceneId = 1, Order = 1, Speaker = "D", Text = "T1", StepType = "Text" }
        );

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        var result = service.GetSteps(1);

        Assert.Equal(4, result.Count);
        Assert.Equal(1, result[0].Order);
        Assert.Equal(2, result[1].Order);
        Assert.Equal(3, result[2].Order);
        Assert.Equal(0, result[3].Order);
    }

    [Fact]
    public void AddStep_AddsStep()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        var service = new AdminSceneService(dbContext);

        var result = service.AddStep(1, new CreateSceneStepRequest
        {
            Order = 1,
            Speaker = "NPC",
            Text = "Hello",
            StepType = "Text",
            MediaUrl = "m1",
            ChoicesJson = "[\"A\"]"
        });

        Assert.True(result.Id > 0);
        Assert.Equal(1, result.SceneId);
        Assert.Equal(1, result.Order);
        Assert.Equal("NPC", result.Speaker);

        var saved = dbContext.SceneSteps.FirstOrDefault(x => x.Id == result.Id);
        Assert.NotNull(saved);
        Assert.Equal(1, saved!.SceneId);
        Assert.Equal(1, saved.Order);
        Assert.Equal("NPC", saved.Speaker);
    }

    [Fact]
    public void AddStep_WhenPositiveOrderAlreadyExists_Throws()
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
    public void AddStep_WhenOrderIsZero_AndAlreadyExists_ShouldAllow()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 0,
            Speaker = "A",
            Text = "T",
            StepType = "Text"
        });

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        var result = service.AddStep(1, new CreateSceneStepRequest
        {
            Order = 0,
            Speaker = "B",
            Text = "T2",
            StepType = "Text"
        });

        Assert.True(result.Id > 0);
        Assert.Equal(0, result.Order);

        var all = dbContext.SceneSteps.Where(x => x.SceneId == 1 && x.Order == 0).ToList();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void AddStep_WhenOrderIsNegative_ShouldNormalizeToZero()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        var service = new AdminSceneService(dbContext);

        var result = service.AddStep(1, new CreateSceneStepRequest
        {
            Order = -5,
            Speaker = "A",
            Text = "T",
            StepType = "Text"
        });

        Assert.True(result.Id > 0);
        Assert.Equal(0, result.Order);

        var saved = dbContext.SceneSteps.FirstOrDefault(x => x.Id == result.Id);
        Assert.NotNull(saved);
        Assert.Equal(0, saved!.Order);
    }

    [Fact]
    public void UpdateStep_UpdatesStep()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "Old",
            Text = "Old text",
            StepType = "Text"
        });

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        service.UpdateStep(1, 1, new UpdateSceneStepRequest
        {
            Order = 2,
            Speaker = "New",
            Text = "New text",
            StepType = "Text",
            MediaUrl = "m2",
            ChoicesJson = "[\"B\"]"
        });

        var updated = dbContext.SceneSteps.FirstOrDefault(x => x.Id == 1);
        Assert.NotNull(updated);

        Assert.Equal(2, updated!.Order);
        Assert.Equal("New", updated.Speaker);
        Assert.Equal("New text", updated.Text);
        Assert.Equal("m2", updated.MediaUrl);
        Assert.Equal("[\"B\"]", updated.ChoicesJson);
    }

    [Fact]
    public void UpdateStep_WhenSetPositiveOrderDuplicate_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        dbContext.SceneSteps.AddRange(
            new SceneStep { Id = 1, SceneId = 1, Order = 1, Speaker = "A", Text = "T1", StepType = "Text" },
            new SceneStep { Id = 2, SceneId = 1, Order = 2, Speaker = "B", Text = "T2", StepType = "Text" }
        );

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.UpdateStep(1, 1, new UpdateSceneStepRequest
            {
                Order = 2,
                Speaker = "A",
                Text = "T1",
                StepType = "Text"
            });
        });
    }

    [Fact]
    public void UpdateStep_WhenOrderIsNegative_ShouldNormalizeToZero()
    {
        var dbContext = TestDbContextFactory.Create();
        SeedScene(dbContext, sceneId: 1);

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "Old",
            Text = "Old text",
            StepType = "Text"
        });

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        service.UpdateStep(1, 1, new UpdateSceneStepRequest
        {
            Order = -3,
            Speaker = "New",
            Text = "New text",
            StepType = "Text"
        });

        var updated = dbContext.SceneSteps.FirstOrDefault(x => x.Id == 1);
        Assert.NotNull(updated);
        Assert.Equal(0, updated!.Order);
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
    public void DeleteStep_RemovesStep()
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

        service.DeleteStep(1, 1);

        Assert.False(dbContext.SceneSteps.Any(x => x.Id == 1 && x.SceneId == 1));
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

    [Fact]
    public void Create_WhenOrderProvided_ShouldPersistOrderAndCourse()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course { Id = 1, Title = "C1", Description = "D1", IsPublished = true });
        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        var result = service.Create(new CreateSceneRequest
        {
            CourseId = 1,
            Order = 5,
            Title = "Scene X",
            Description = "Desc",
            SceneType = "intro",
            BackgroundUrl = null,
            AudioUrl = null,
            Steps = new()
        });

        var savedScene = dbContext.Scenes.FirstOrDefault(x => x.Id == result.Id);
        Assert.NotNull(savedScene);
        Assert.Equal(1, savedScene!.CourseId);
        Assert.Equal(5, savedScene.Order);
    }

    [Fact]
    public void Create_WhenDuplicateOrderInSameCourse_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course { Id = 1, Title = "C1", Description = "D1", IsPublished = true });

        dbContext.Scenes.Add(new Scene
        {
            Id = 10,
            CourseId = 1,
            Order = 3,
            Title = "S1",
            Description = "D",
            SceneType = "intro"
        });

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.Create(new CreateSceneRequest
            {
                CourseId = 1,
                Order = 3,
                Title = "S2",
                Description = "D",
                SceneType = "intro",
                BackgroundUrl = null,
                AudioUrl = null,
                Steps = new()
            });
        });
    }

    [Fact]
    public void Update_WhenDuplicateOrderInSameCourse_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course { Id = 1, Title = "C1", Description = "D1", IsPublished = true });

        dbContext.Scenes.AddRange(
            new Scene { Id = 11, CourseId = 1, Order = 1, Title = "S1", Description = "D", SceneType = "intro" },
            new Scene { Id = 12, CourseId = 1, Order = 2, Title = "S2", Description = "D", SceneType = "intro" }
        );

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.Update(12, new UpdateSceneRequest
            {
                CourseId = 1,
                Order = 1,
                Title = "S2",
                Description = "D",
                SceneType = "intro",
                BackgroundUrl = null,
                AudioUrl = null
            });
        });
    }

    [Fact]
    public void Update_WhenMoveToCourseWithDuplicateOrder_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.AddRange(
            new Course { Id = 1, Title = "C1", Description = "D1", IsPublished = true },
            new Course { Id = 2, Title = "C2", Description = "D2", IsPublished = true }
        );

        dbContext.Scenes.AddRange(
            new Scene { Id = 21, CourseId = 1, Order = 1, Title = "A-1", Description = "D", SceneType = "intro" },
            new Scene { Id = 22, CourseId = 2, Order = 1, Title = "B-1", Description = "D", SceneType = "intro" }
        );

        dbContext.SaveChanges();

        var service = new AdminSceneService(dbContext);

        Assert.Throws<ArgumentException>(() =>
        {
            service.Update(21, new UpdateSceneRequest
            {
                CourseId = 2,
                Order = 1,
                Title = "A-1",
                Description = "D",
                SceneType = "intro",
                BackgroundUrl = null,
                AudioUrl = null
            });
        });
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
