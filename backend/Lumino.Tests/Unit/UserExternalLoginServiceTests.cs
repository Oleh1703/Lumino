using System;
using System.Linq;
using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Lumino.Tests.Stubs;
using Xunit;

namespace Lumino.Tests;

public class UserExternalLoginServiceTests
{
    [Fact]
    public void UnlinkExternalLogin_WhenLastExternalLoginAndNoPasswordResetAndNotLinkedLater_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        var user = new User
        {
            Id = 10,
            Email = "a@a.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.UserExternalLogins.Add(new UserExternalLogin
        {
            UserId = 10,
            Provider = "google",
            ProviderUserId = "sub",
            Email = "a@a.com",
            CreatedAtUtc = user.CreatedAt.AddSeconds(10)
        });
        dbContext.SaveChanges();

        var service = new UserExternalLoginService(dbContext, new FakeOpenIdTokenValidator());

        var ex = Assert.Throws<ForbiddenAccessException>(() =>
            service.UnlinkExternalLogin(10, new UnlinkExternalLoginRequest { Provider = "google" })
        );

        Assert.Contains("Не можна відв’язати останній спосіб входу", ex.Message);
    }

    [Fact]
    public void UnlinkExternalLogin_WhenLastExternalLoginButPasswordResetUsed_ShouldAllow()
    {
        var dbContext = TestDbContextFactory.Create();

        var createdAt = DateTime.UtcNow;

        dbContext.Users.Add(new User
        {
            Id = 10,
            Email = "a@a.com",
            PasswordHash = "hash",
            CreatedAt = createdAt
        });

        dbContext.UserExternalLogins.Add(new UserExternalLogin
        {
            UserId = 10,
            Provider = "google",
            ProviderUserId = "sub",
            Email = "a@a.com",
            CreatedAtUtc = createdAt.AddSeconds(10)
        });

        dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = 10,
            TokenHash = "t",
            CreatedAt = createdAt,
            ExpiresAt = createdAt.AddHours(1),
            UsedAt = createdAt.AddMinutes(1)
        });

        dbContext.SaveChanges();

        var service = new UserExternalLoginService(dbContext, new FakeOpenIdTokenValidator());

        service.UnlinkExternalLogin(10, new UnlinkExternalLoginRequest { Provider = "google" });

        Assert.Empty(dbContext.UserExternalLogins.Where(x => x.UserId == 10));
    }

    [Fact]
    public void GetExternalLogins_ShouldReturnOrderedList()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 10,
            Email = "a@a.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.UserExternalLogins.Add(new UserExternalLogin
        {
            UserId = 10,
            Provider = "google_alt",
            ProviderUserId = "sub2",
            Email = "a@a.com",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(2)
        });

        dbContext.UserExternalLogins.Add(new UserExternalLogin
        {
            UserId = 10,
            Provider = "google",
            ProviderUserId = "sub1",
            Email = "a@a.com",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        });

        dbContext.SaveChanges();

        var service = new UserExternalLoginService(dbContext, new FakeOpenIdTokenValidator());

        var result = service.GetExternalLogins(10);

        Assert.Equal(2, result.Count);
        Assert.Equal("google", result[0].Provider);
        Assert.Equal("google_alt", result[1].Provider);
    }

    [Fact]
    public void LinkGoogleExternalLogin_WhenNotLinked_ShouldCreateExternalLogin()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 10,
            Email = "a@a.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var validator = new FakeOpenIdTokenValidator
        {
            GoogleUserInfo = new OpenIdUserInfo
            {
                Subject = "g-sub",
                Email = "a@a.com",
                Name = "n",
                PictureUrl = null
            }
        };

        var service = new UserExternalLoginService(dbContext, validator);

        service.LinkGoogleExternalLogin(10, new LinkExternalLoginRequest { IdToken = "token" });

        Assert.Single(dbContext.UserExternalLogins.Where(x => x.UserId == 10 && x.Provider == "google"));
    }

    [Fact]
    public void LinkGoogleExternalLogin_WhenProviderUserIdLinkedToAnotherUser_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 10,
            Email = "a@a.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Users.Add(new User
        {
            Id = 11,
            Email = "b@b.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.UserExternalLogins.Add(new UserExternalLogin
        {
            UserId = 11,
            Provider = "google",
            ProviderUserId = "g-sub",
            Email = "b@b.com",
            CreatedAtUtc = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var validator = new FakeOpenIdTokenValidator
        {
            GoogleUserInfo = new OpenIdUserInfo
            {
                Subject = "g-sub",
                Email = "a@a.com",
                Name = "n",
                PictureUrl = null
            }
        };

        var service = new UserExternalLoginService(dbContext, validator);

        Assert.Throws<ForbiddenAccessException>(() =>
            service.LinkGoogleExternalLogin(10, new LinkExternalLoginRequest { IdToken = "token" })
        );
    }
}
