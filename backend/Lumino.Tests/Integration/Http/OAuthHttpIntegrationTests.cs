using Lumino.Api.Data;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class OAuthHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public OAuthHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OAuthGoogle_WhenNewUser_ShouldCreateUserAndExternalLogin_AndReturnTokens()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/oauth/google", new
        {
            idToken = "test-google:google-sub-1:google.user@test.local"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<Lumino.Api.Application.DTOs.AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            var user = dbContext.Users.SingleOrDefault(x => x.Email == "google.user@test.local");
            Assert.NotNull(user);

            var ext = dbContext.UserExternalLogins.SingleOrDefault(x => x.UserId == user!.Id && x.Provider == "google");
            Assert.NotNull(ext);
            Assert.Equal("google-sub-1", ext!.ProviderUserId);
        }
    }

    [Fact]
    public async Task OAuthApple_WhenExistingEmail_ShouldLinkExternalLogin_AndReturnTokens()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "apple.user@test.local",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = System.DateTime.UtcNow,
                Username = "apple_user",
                Hearts = 5,
                Crystals = 0,
                Theme = "light"
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/oauth/apple", new
        {
            idToken = "test-apple:apple-sub-1:apple.user@test.local"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<Lumino.Api.Application.DTOs.AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            var user = dbContext.Users.SingleOrDefault(x => x.Email == "apple.user@test.local");
            Assert.NotNull(user);

            var ext = dbContext.UserExternalLogins.SingleOrDefault(x => x.UserId == user!.Id && x.Provider == "apple");
            Assert.NotNull(ext);
            Assert.Equal("apple-sub-1", ext!.ProviderUserId);
        }
    }

    [Fact]
    public async Task OAuthApple_WhenEmailIsMissing_ButExternalLoginExists_ShouldLogin_AndReturnTokens()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "apple.noemail@test.local",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = System.DateTime.UtcNow,
                Username = "apple_noemail",
                Hearts = 5,
                Crystals = 0,
                Theme = "light"
            });

            dbContext.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = 10,
                Provider = "apple",
                ProviderUserId = "apple-sub-no-email",
                Email = null,
                CreatedAtUtc = System.DateTime.UtcNow
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/oauth/apple", new
        {
            idToken = "test-apple:apple-sub-no-email"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<Lumino.Api.Application.DTOs.AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
    }

    [Fact]
    public async Task OAuthApple_WhenEmailIsMissing_AndExternalLoginDoesNotExist_ShouldReturnForbidden()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/oauth/apple", new
        {
            idToken = "test-apple:apple-sub-no-email-2"
        });

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
        Assert.Contains("Apple не передав email", detail);
    }
}
