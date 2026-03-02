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
}
