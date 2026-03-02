using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class ExternalLoginsHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalLoginsHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetExternalLogins_ShouldReturnProviders()
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
                CreatedAt = DateTime.UtcNow
            });

            dbContext.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = 10,
                Provider = "google",
                ProviderUserId = "g-1",
                Email = "test@test.com",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/user/external-logins");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(1, root.GetArrayLength());

        var providers = root.EnumerateArray().Select(x => x.GetProperty("provider").GetString()).ToList();
        Assert.Contains("google", providers);
    }

    [Fact]
    public async Task UnlinkExternalLogin_WhenMultiple_ShouldRemoveAndReturnNoContent()
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
                CreatedAt = DateTime.UtcNow
            });

            dbContext.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = 10,
                Provider = "google",
                ProviderUserId = "g-1",
                Email = "test@test.com",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            });

            // Додаємо ще один external login, щоб "google" не був останнім способом входу.
            // (Apple-логін як провайдер прибраний з бекенду, але в БД можуть лишатися старі прив'язки.)
            dbContext.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = 10,
                Provider = "apple",
                ProviderUserId = "a-1",
                Email = "test@test.com",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3)
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/user/external-logins/unlink", new { provider = "google" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();
            var count = dbContext.UserExternalLogins.Count(x => x.UserId == 10);
            Assert.Equal(1, count);
            Assert.DoesNotContain(dbContext.UserExternalLogins.Where(x => x.UserId == 10), x => x.Provider == "google");
        }
    }

    [Fact]
    public async Task UnlinkExternalLogin_WhenLastAndOAuthOnly_ShouldReturnForbiddenProblemDetails()
    {
        var now = DateTime.UtcNow;

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

            // Один external login, створений майже одночасно з юзером (OAuth-first)
            dbContext.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = 10,
                Provider = "google",
                ProviderUserId = "g-1",
                Email = "test@test.com",
                CreatedAtUtc = now.AddSeconds(30)
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/user/external-logins/unlink", new { provider = "google" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.ToString() ?? "");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("type", out _));
        Assert.True(root.TryGetProperty("title", out _));
        Assert.True(root.TryGetProperty("status", out _));
        Assert.True(root.TryGetProperty("detail", out _));
        Assert.True(root.TryGetProperty("instance", out _));
        Assert.True(root.TryGetProperty("traceId", out _));

        Assert.Equal("forbidden", root.GetProperty("type").GetString());
        Assert.Equal(403, root.GetProperty("status").GetInt32());

        var detail = root.GetProperty("detail").GetString() ?? "";
        Assert.Contains("Не можна відв’язати останній спосіб входу", detail);
    }

    [Fact]
    public async Task UnlinkExternalLogin_WhenLastButPasswordWasSet_ShouldAllow()
    {
        var now = DateTime.UtcNow;

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

            dbContext.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = 10,
                Provider = "google",
                ProviderUserId = "g-1",
                Email = "test@test.com",
                CreatedAtUtc = now.AddSeconds(30)
            });

            // Позначаємо, що користувач колись робив reset password і встановив пароль
            dbContext.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = 10,
                TokenHash = "hash",
                CreatedAt = now.AddDays(-1),
                ExpiresAt = now.AddDays(-1).AddHours(1),
                UsedAt = now.AddDays(-1).AddMinutes(1)
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/user/external-logins/unlink", new { provider = "google" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
