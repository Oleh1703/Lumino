using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class UserProfileUpdateConsistencyHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public UserProfileUpdateConsistencyHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnStreakAndEconomyFields_ConsistentWithGetMe()
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
                Theme = "light",
                HeartsUpdatedAtUtc = DateTime.UtcNow
            });

            dbContext.UserStreaks.Add(new UserStreak
            {
                Id = 1,
                UserId = 10,
                CurrentStreak = 5,
                BestStreak = 7,
                LastActivityDateUtc = DateTime.UtcNow.Date
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var updateResponse = await client.PutAsJsonAsync("/api/user/profile", new { theme = "dark" });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var json = await updateResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;

        AssertJsonHasProperties(
            root,
            "id",
            "email",
            "role",
            "createdAt",
            "username",
            "avatarUrl",
            "hearts",
            "crystals",
            "theme",
            "heartsMax",
            "heartRegenMinutes",
            "crystalCostPerHeart",
            "nextHeartAtUtc",
            "nextHeartInSeconds",
            "currentStreakDays",
            "bestStreakDays"
        );

        Assert.Equal("dark", root.GetProperty("theme").GetString());

        Assert.Equal(5, root.GetProperty("hearts").GetInt32());
        Assert.Equal(12, root.GetProperty("crystals").GetInt32());

        Assert.True(root.GetProperty("heartsMax").GetInt32() > 0);
        Assert.True(root.GetProperty("heartRegenMinutes").GetInt32() > 0);
        Assert.True(root.GetProperty("crystalCostPerHeart").GetInt32() > 0);

        // hearts == heartsMax => nextHeartAtUtc null and 0 seconds
        Assert.True(root.GetProperty("nextHeartAtUtc").ValueKind == JsonValueKind.Null);
        Assert.Equal(0, root.GetProperty("nextHeartInSeconds").GetInt32());

        Assert.Equal(5, root.GetProperty("currentStreakDays").GetInt32());
        Assert.Equal(7, root.GetProperty("bestStreakDays").GetInt32());
    }

    private static void AssertJsonHasProperties(JsonElement element, params string[] properties)
    {
        foreach (var prop in properties)
        {
            Assert.True(element.TryGetProperty(prop, out _), $"Missing property '{prop}'");
        }
    }
}
