using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Tests.Stubs;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class ForgotPasswordEmailHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ForgotPasswordEmailHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgotPassword_WhenUserExists_ShouldSendEmail_AndNotExposeTokenInResponse()
    {
        int beforeCalls;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "test@lumino.com",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = DateTime.UtcNow,
                Username = "test",
                Hearts = 5,
                Crystals = 0,
                Theme = "light"
            });

            dbContext.SaveChanges();

            var sender = (FakeEmailSender)scope.ServiceProvider.GetRequiredService<IEmailSender>();
            beforeCalls = sender.SendCallsCount;
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "test@lumino.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("isSent", out var isSent));
        Assert.True(isSent.GetBoolean());

        // Відповідь не повинна містити токен (security)
        Assert.True(doc.RootElement.TryGetProperty("resetToken", out var resetToken));
        Assert.True(resetToken.ValueKind == JsonValueKind.Null);

        Assert.True(doc.RootElement.TryGetProperty("expiresAtUtc", out var expiresAtUtc));
        Assert.True(expiresAtUtc.ValueKind == JsonValueKind.Null);

        using (var scope = _factory.Services.CreateScope())
        {
            var sender = (FakeEmailSender)scope.ServiceProvider.GetRequiredService<IEmailSender>();

            Assert.Equal(beforeCalls + 1, sender.SendCallsCount);
            Assert.Equal("test@lumino.com", sender.LastToEmail);
            Assert.False(string.IsNullOrWhiteSpace(sender.LastHtmlBody));

            // Лист має містити або посилання, або token (для ручного введення у фронті)
            Assert.Contains("reset", sender.LastHtmlBody!, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ForgotPassword_WhenUserNotFound_ShouldReturnOk_AndNotSendEmail()
    {
        int beforeCalls;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var sender = (FakeEmailSender)scope.ServiceProvider.GetRequiredService<IEmailSender>();
            beforeCalls = sender.SendCallsCount;
        }

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "missing@lumino.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("isSent", out var isSent));
        Assert.True(isSent.GetBoolean());

        using (var scope = _factory.Services.CreateScope())
        {
            var sender = (FakeEmailSender)scope.ServiceProvider.GetRequiredService<IEmailSender>();
            Assert.Equal(beforeCalls, sender.SendCallsCount);
        }
    }
}
