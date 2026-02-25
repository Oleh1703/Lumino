using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class ExternalLoginsLinkHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalLoginsLinkHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LinkGoogleExternalLogin_ShouldCreateExternalLogin()
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

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/user/external-logins/link/google",
            new { idToken = "test-google:g-sub:test@test.com" }
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();
            Assert.Single(dbContext.UserExternalLogins.Where(x => x.UserId == 10 && x.Provider == "google"));
        }
    }

    [Fact]
    public async Task LinkGoogleExternalLogin_WhenProviderUserIdAlreadyLinkedToAnotherUser_ShouldReturnForbidden()
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

            dbContext.Users.Add(new User
            {
                Id = 11,
                Email = "other@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            });

            dbContext.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = 11,
                Provider = "google",
                ProviderUserId = "g-sub",
                Email = "other@test.com",
                CreatedAtUtc = DateTime.UtcNow
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/user/external-logins/link/google",
            new { idToken = "test-google:g-sub:test@test.com" }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
