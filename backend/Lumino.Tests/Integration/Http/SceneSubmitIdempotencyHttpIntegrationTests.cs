using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class SceneSubmitIdempotencyHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public SceneSubmitIdempotencyHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitScene_WhenSameIdempotencyKey_ShouldReturnSameResult_AndNotOverrideAttempt()
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
                PasswordHash = "hash",                CreatedAt = DateTime.UtcNow
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
                Text = "Q",
                StepType = "Choice",
                MediaUrl = null,
                ChoicesJson = "[{\"text\":\"Paris\",\"isCorrect\":true},{\"text\":\"London\",\"isCorrect\":false}]"
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var request1 = new
        {
            idempotencyKey = "scene-key-1",
            answers = new[]
            {
                new { stepId = 1, answer = "Paris" }
            }
        };

        var json1 = JsonSerializer.Serialize(request1);
        var response1 = await client.PostAsync("/api/scenes/1/submit", new StringContent(json1, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var body1 = await response1.Content.ReadAsStringAsync();
        using var doc1 = JsonDocument.Parse(body1);

        Assert.True(doc1.RootElement.GetProperty("isCompleted").GetBoolean());
        Assert.Equal(1, doc1.RootElement.GetProperty("totalQuestions").GetInt32());
        Assert.Equal(1, doc1.RootElement.GetProperty("correctAnswers").GetInt32());

        var request2 = new
        {
            idempotencyKey = "scene-key-1",
            answers = new[]
            {
                new { stepId = 1, answer = "London" }
            }
        };

        var json2 = JsonSerializer.Serialize(request2);
        var response2 = await client.PostAsync("/api/scenes/1/submit", new StringContent(json2, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var body2 = await response2.Content.ReadAsStringAsync();
        using var doc2 = JsonDocument.Parse(body2);

        // MUST return exactly the first result (idempotency)
        Assert.True(doc2.RootElement.GetProperty("isCompleted").GetBoolean());
        Assert.Equal(1, doc2.RootElement.GetProperty("totalQuestions").GetInt32());
        Assert.Equal(1, doc2.RootElement.GetProperty("correctAnswers").GetInt32());

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            var attempt = dbContext.SceneAttempts
                .FirstOrDefault(x => x.UserId == 10 && x.SceneId == 1);

            Assert.NotNull(attempt);

            Assert.Equal(1, attempt!.Score);
            Assert.Equal(1, attempt.TotalQuestions);
            Assert.True(attempt.IsCompleted);
            Assert.Equal("scene-key-1", attempt.SubmitIdempotencyKey);
            Assert.Equal("scene-key-1", attempt.IdempotencyKey);
            Assert.True(string.IsNullOrWhiteSpace(attempt.MistakesIdempotencyKey));

            Assert.False(string.IsNullOrWhiteSpace(attempt.DetailsJson));
            Assert.Contains("Paris", attempt.DetailsJson!);
        }
    }
}
