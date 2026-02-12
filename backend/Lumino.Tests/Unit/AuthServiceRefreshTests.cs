using System.Security.Cryptography;
using System.Text;
using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lumino.Tests;

public class AuthServiceRefreshTests
{
    [Fact]
    public void Refresh_ShouldReturnNewTokens_AndRevokeOld()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = CreateConfiguration(maxActiveTokens: 3, expiresDays: 7);

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new PasswordHasher()
        );

        var register = service.Register(new RegisterRequest
        {
            Email = "refresh@mail.com",
            Password = "123456"
        });

        var refreshed = service.Refresh(new RefreshTokenRequest
        {
            RefreshToken = register.RefreshToken
        });

        Assert.False(string.IsNullOrWhiteSpace(refreshed.Token));
        Assert.False(string.IsNullOrWhiteSpace(refreshed.RefreshToken));
        Assert.NotEqual(register.RefreshToken, refreshed.RefreshToken);

        var active = dbContext.RefreshTokens.Where(x => x.RevokedAt == null).ToList();
        var revoked = dbContext.RefreshTokens.Where(x => x.RevokedAt != null).ToList();

        Assert.Single(active);
        Assert.Single(revoked);

        Assert.False(string.IsNullOrWhiteSpace(revoked[0].ReplacedByTokenHash));
        Assert.Equal(active[0].TokenHash, revoked[0].ReplacedByTokenHash);
    }

    [Fact]
    public void Refresh_InvalidToken_ShouldThrowUnauthorized()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = CreateConfiguration(maxActiveTokens: 3, expiresDays: 7);

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new PasswordHasher()
        );

        Assert.Throws<UnauthorizedAccessException>(() =>
        {
            service.Refresh(new RefreshTokenRequest
            {
                RefreshToken = "THIS_TOKEN_DOES_NOT_EXIST"
            });
        });
    }

    [Fact]
    public void Refresh_RevokedToken_ShouldThrowUnauthorized()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = CreateConfiguration(maxActiveTokens: 3, expiresDays: 7);

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new PasswordHasher()
        );

        var register = service.Register(new RegisterRequest
        {
            Email = "revoked@mail.com",
            Password = "123456"
        });

        service.Logout(new RefreshTokenRequest
        {
            RefreshToken = register.RefreshToken
        });

        Assert.Throws<UnauthorizedAccessException>(() =>
        {
            service.Refresh(new RefreshTokenRequest
            {
                RefreshToken = register.RefreshToken
            });
        });
    }

    [Fact]
    public void Refresh_ExpiredToken_ShouldThrowUnauthorized()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = CreateConfiguration(maxActiveTokens: 3, expiresDays: 7);

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new PasswordHasher()
        );

        var register = service.Register(new RegisterRequest
        {
            Email = "expired@mail.com",
            Password = "123456"
        });

        var tokenEntity = dbContext.RefreshTokens.First();
        tokenEntity.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        dbContext.SaveChanges();

        Assert.Throws<UnauthorizedAccessException>(() =>
        {
            service.Refresh(new RefreshTokenRequest
            {
                RefreshToken = register.RefreshToken
            });
        });
    }

    [Fact]
    public void LimitActiveTokens_ShouldRevokeOldTokens_WhenOverLimit()
    {
        var dbContext = TestDbContextFactory.Create();

        // Макс 1 активний токен
        var configuration = CreateConfiguration(maxActiveTokens: 1, expiresDays: 7);

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new PasswordHasher()
        );

        var register = service.Register(new RegisterRequest
        {
            Email = "limit@mail.com",
            Password = "123456"
        });

        // невелика пауза, щоб CreatedAt точно відрізнявся
        Thread.Sleep(10);

        var login = service.Login(new LoginRequest
        {
            Email = "limit@mail.com",
            Password = "123456"
        });

        Assert.NotEqual(register.RefreshToken, login.RefreshToken);

        var active = dbContext.RefreshTokens.Where(x => x.RevokedAt == null).ToList();
        var revoked = dbContext.RefreshTokens.Where(x => x.RevokedAt != null).ToList();

        Assert.Single(active);
        Assert.Single(revoked);

        var newest = dbContext.RefreshTokens.OrderByDescending(x => x.CreatedAt).First();
        Assert.Equal(newest.Id, active[0].Id);
    }

    [Fact]
    public void Logout_ShouldRevokeToken_AndBeIdempotent()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = CreateConfiguration(maxActiveTokens: 3, expiresDays: 7);

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new PasswordHasher()
        );

        var register = service.Register(new RegisterRequest
        {
            Email = "logout@mail.com",
            Password = "123456"
        });

        service.Logout(new RefreshTokenRequest { RefreshToken = register.RefreshToken });
        service.Logout(new RefreshTokenRequest { RefreshToken = register.RefreshToken });

        var revokedCount = dbContext.RefreshTokens.Count(x => x.RevokedAt != null);
        Assert.Equal(1, revokedCount);
    }

    [Fact]
    public void Login_LegacySha256Hash_ShouldAutoUpgradeToPbkdf2()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = CreateConfiguration(maxActiveTokens: 3, expiresDays: 7);

        var password = "123456";
        var legacyHash = ComputeLegacySha256Base64(password);

        dbContext.Users.Add(new User
        {
            Email = "legacy@mail.com",
            PasswordHash = legacyHash,
            CreatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new PasswordHasher()
        );

        var response = service.Login(new LoginRequest
        {
            Email = "legacy@mail.com",
            Password = password
        });

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));

        var user = dbContext.Users.First(x => x.Email == "legacy@mail.com");
        Assert.StartsWith("pbkdf2.v1.", user.PasswordHash);

        var hasher = new PasswordHasher();
        Assert.True(hasher.Verify(password, user.PasswordHash));
    }

    private static IConfiguration CreateConfiguration(int maxActiveTokens, int expiresDays)
    {
        var data = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "TEST_KEY_1234567890_TEST_KEY_1234567890",
            ["Jwt:Issuer"] = "Lumino.Test",
            ["Jwt:Audience"] = "Lumino.Test",
            ["Jwt:ExpiresMinutes"] = "60",

            ["RefreshToken:MaxActiveTokens"] = maxActiveTokens.ToString(),
            ["RefreshToken:ExpiresDays"] = expiresDays.ToString(),
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private static string ComputeLegacySha256Base64(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
