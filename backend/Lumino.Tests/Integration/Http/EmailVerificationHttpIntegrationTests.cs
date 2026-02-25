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

public class EmailVerificationHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public EmailVerificationHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ShouldSendVerificationEmail_AndNotReturnTokens()
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

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "verify@lumino.com",
            password = "123456",
            username = "verifier"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("requiresEmailVerification", out var requires));
        Assert.True(requires.GetBoolean());

        Assert.True(doc.RootElement.TryGetProperty("token", out var token));
        Assert.True(token.ValueKind == JsonValueKind.Null);

        Assert.True(doc.RootElement.TryGetProperty("refreshToken", out var refresh));
        Assert.True(refresh.ValueKind == JsonValueKind.Null);

        using (var scope = _factory.Services.CreateScope())
        {
            var sender = (FakeEmailSender)scope.ServiceProvider.GetRequiredService<IEmailSender>();
            Assert.Equal(beforeCalls + 1, sender.SendCallsCount);
            Assert.Equal("verify@lumino.com", sender.LastToEmail);
            Assert.Contains("verify", sender.LastHtmlBody!, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturnTokens_AndAllowLogin()
    {
        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            // seed unverified user + token
            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "u@lumino.com",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = DateTime.UtcNow,
                Username = "u",
                IsEmailVerified = false,
                Hearts = 5,
                Crystals = 0,
                Theme = "light"
            });

            dbContext.SaveChanges();

            // Use AuthService to generate a real verification email/token, so we don't depend on token format.
            var auth = scope.ServiceProvider.GetRequiredService<Lumino.Api.Application.Interfaces.IAuthService>();
            var sender = (FakeEmailSender)scope.ServiceProvider.GetRequiredService<IEmailSender>();

            // Resend will create token and send
            auth.ResendVerification(new Lumino.Api.Application.DTOs.ResendVerificationRequest { Email = "u@lumino.com" }, null, null);

            token = ExtractTokenFromEmailBody(sender.LastHtmlBody);
        }

        var client = _factory.CreateClient();

        var verify = await client.PostAsJsonAsync("/api/auth/verify-email", new { token = token });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        var verifyJson = await verify.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(verifyJson))
        {
            Assert.True(doc.RootElement.TryGetProperty("requiresEmailVerification", out var requires));
            Assert.False(requires.GetBoolean());

            Assert.True(doc.RootElement.TryGetProperty("token", out var jwt));
            Assert.False(string.IsNullOrWhiteSpace(jwt.GetString()));

            Assert.True(doc.RootElement.TryGetProperty("refreshToken", out var refresh));
            Assert.False(string.IsNullOrWhiteSpace(refresh.GetString()));
        }
    }

    private static string ExtractTokenFromEmailBody(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException("Email body is empty");
        }

        var marker = "token=";
        var idx = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var start = idx + marker.Length;
            var end = html.IndexOf('"', start);
            if (end < 0)
            {
                end = html.IndexOf('<', start);
            }

            if (end > start)
            {
                return Uri.UnescapeDataString(html.Substring(start, end - start));
            }
        }

        var bOpen = html.IndexOf("<b>", StringComparison.OrdinalIgnoreCase);
        var bClose = html.IndexOf("</b>", StringComparison.OrdinalIgnoreCase);
        if (bOpen >= 0 && bClose > bOpen)
        {
            var raw = html.Substring(bOpen + 3, bClose - (bOpen + 3));
            if (!string.IsNullOrWhiteSpace(raw))
            {
                return raw.Trim();
            }
        }

        throw new InvalidOperationException("Cannot extract token from email body");
    }
}
