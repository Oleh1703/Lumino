using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class SceneCompleteIdempotencyHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public SceneCompleteIdempotencyHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteScene_WhenAttemptExistsButNotCompleted_ShouldCompleteIt_AndSecondCallShouldBeNoOp()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "test@test.com",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = DateTime.UtcNow
            });

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

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
                Text = "Hello",
                StepType = "Dialog",
                MediaUrl = null,
                ChoicesJson = null
            });

            dbContext.SceneAttempts.Add(new SceneAttempt
            {
                UserId = 10,
                SceneId = 1,
                IsCompleted = false,
                CompletedAt = default(DateTime),
                Score = 0,
                TotalQuestions = 0,
                DetailsJson = null
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var req = new { sceneId = 1 };
        var json = JsonSerializer.Serialize(req);

        var r1 = await client.PostAsync("/api/scenes/complete", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NoContent, r1.StatusCode);

        var r2 = await client.PostAsync("/api/scenes/complete", new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NoContent, r2.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            var attempt = dbContext.SceneAttempts
                .FirstOrDefault(x => x.UserId == 10 && x.SceneId == 1);

            Assert.NotNull(attempt);

            Assert.True(attempt!.IsCompleted);
            Assert.NotEqual(default(DateTime), attempt.CompletedAt);
        }
    }
}
