using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class ProfileAndProgressHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ProfileAndProgressHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMe_ShouldReturnProfile_WithNewFields()
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
                CreatedAt = DateTime.UtcNow,
                Username = "tester",
                AvatarUrl = "/avatars/alien-1.png",
                Hearts = 5,
                Crystals = 12,
                Theme = "dark"
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/user/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;

        AssertJsonHasProperties(root, "id", "email", "role", "createdAt", "username", "avatarUrl", "hearts", "crystals", "theme");

        Assert.Equal("tester", root.GetProperty("username").GetString());
        Assert.Equal("/avatars/alien-1.png", root.GetProperty("avatarUrl").GetString());
        Assert.Equal(5, root.GetProperty("hearts").GetInt32());
        Assert.Equal(12, root.GetProperty("crystals").GetInt32());
        Assert.Equal("dark", root.GetProperty("theme").GetString());
    }

    [Fact]
    public async Task ProgressMe_ShouldReturnWeeklyScores_7Days()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

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
                CreatedAt = now
            });

            dbContext.LessonResults.Add(new LessonResult
            {
                Id = 1,
                UserId = 10,
                LessonId = 1,
                Score = 10,
                TotalQuestions = 10,
                CompletedAt = today
            });

            dbContext.SceneAttempts.Add(new SceneAttempt
            {
                Id = 1,
                UserId = 10,
                SceneId = 1,
                IsCompleted = true,
                Score = 5,
                TotalQuestions = 5,
                CompletedAt = today.AddDays(-1)
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/progress/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        AssertJsonHasProperties(root, "weeklyScores");

        var weekly = root.GetProperty("weeklyScores");
        Assert.Equal(JsonValueKind.Array, weekly.ValueKind);
        Assert.Equal(7, weekly.GetArrayLength());

        AssertJsonHasProperties(weekly[0], "dateUtc", "score");
    }

    private static void AssertJsonHasProperties(JsonElement element, params string[] properties)
    {
        foreach (var prop in properties)
        {
            Assert.True(element.TryGetProperty(prop, out _), $"Missing property '{prop}'");
        }
    }
}
