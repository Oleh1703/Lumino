using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Lumino.Tests.Stubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Lumino.Tests.Unit;

public class AuthForgotPasswordEmailTests
{
    [Fact]
    public void ForgotPassword_WhenUserExists_ShouldSendEmail_AndNotExposeToken()
    {
        var options = new DbContextOptionsBuilder<LuminoDbContext>()
            .UseInMemoryDatabase("ForgotPasswordEmail_" + Guid.NewGuid().ToString("N"))
            .Options;

        using var db = new LuminoDbContext(options);

        var user = new User
        {
            Email = "test@lumino.com",
            PasswordHash = "hash",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow,
            Username = "test",
            AvatarUrl = null
        };

        db.Users.Add(user);
        db.SaveChanges();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "CHANGE_ME",
                ["Jwt:Issuer"] = "Lumino.Api",
                ["Jwt:Audience"] = "Lumino.Client",
                ["Jwt:ExpiresMinutes"] = "60",
                ["Email:FrontendBaseUrl"] = "http://localhost:5173"
            })
            .Build();

        var registerValidator = new FakeRegisterValidator();
        var loginValidator = new FakeLoginValidator();
        var forgotValidator = new FakeForgotPasswordValidator();
        var resetValidator = new FakeResetPasswordValidator();

        var env = new TestHostEnvironment("Production");

        var fakeEmail = new FakeEmailSender();
        var hasher = new PasswordHasher();
        var openIdValidator = new FakeOpenIdTokenValidator();

        var service = new AuthService(db, config, registerValidator, loginValidator, forgotValidator, resetValidator, fakeEmail, openIdValidator, env, hasher);

        var response = service.ForgotPassword(new ForgotPasswordRequest { Email = "test@lumino.com" }, null, null);

        Assert.True(response.IsSent);
        Assert.Null(response.ResetToken);
        Assert.Null(response.ExpiresAtUtc);

        Assert.Equal(1, fakeEmail.SendCallsCount);
        Assert.Equal("test@lumino.com", fakeEmail.LastToEmail);
        Assert.False(string.IsNullOrWhiteSpace(fakeEmail.LastHtmlBody));
    }
}
