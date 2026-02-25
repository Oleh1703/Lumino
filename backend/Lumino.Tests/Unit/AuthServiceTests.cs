using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Xunit;
using Lumino.Tests.Stubs;

namespace Lumino.Tests;

public class AuthServiceTests
{
    [Fact]
    public void Register_ShouldCreateUser_AndRequireEmailVerification()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        var emailSender = new FakeEmailSender();

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new ForgotPasswordRequestValidator(),
            new ResetPasswordRequestValidator(),
            new VerifyEmailRequestValidator(),
            new ResendVerificationRequestValidator(),
            emailSender,
            new FakeOpenIdTokenValidator(),
            new FakeHostEnvironment(),
            new PasswordHasher()
        );

        var response = service.Register(new RegisterRequest
        {
            Email = "test@mail.com",
            Password = "123456"
        });

        Assert.True(response.RequiresEmailVerification);
        Assert.True(string.IsNullOrWhiteSpace(response.Token));
        Assert.True(string.IsNullOrWhiteSpace(response.RefreshToken));

        Assert.Equal(1, emailSender.SendCallsCount);

        var user = dbContext.Users.FirstOrDefault(x => x.Email == "test@mail.com");
        Assert.NotNull(user);
        Assert.False(string.IsNullOrWhiteSpace(user!.PasswordHash));
        Assert.False(user.IsEmailVerified);
    }

    [Fact]
    public void Register_DuplicateEmail_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        dbContext.Users.Add(new User
        {
            Email = "test@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = true
        });

        dbContext.SaveChanges();

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new ForgotPasswordRequestValidator(),
            new ResetPasswordRequestValidator(),
            new VerifyEmailRequestValidator(),
            new ResendVerificationRequestValidator(),
            new FakeEmailSender(),
            new FakeOpenIdTokenValidator(),
            new FakeHostEnvironment(),
            new PasswordHasher()
        );

        Assert.Throws<ArgumentException>(() =>
        {
            service.Register(new RegisterRequest
            {
                Email = "test@mail.com",
                Password = "123456"
            });
        });
    }

    
    [Fact]
    public void Register_ShouldSaveLanguages_WhenProvided()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        var emailSender = new FakeEmailSender();

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new ForgotPasswordRequestValidator(),
            new ResetPasswordRequestValidator(),
            new VerifyEmailRequestValidator(),
            new ResendVerificationRequestValidator(),
            emailSender,
            new FakeOpenIdTokenValidator(),
            new FakeHostEnvironment(),
            new PasswordHasher()
        );

        service.Register(new RegisterRequest
        {
            Email = "lang@mail.com",
            Password = "123456",
            NativeLanguageCode = "uk",
            TargetLanguageCode = "en"
        });

        var user = dbContext.Users.FirstOrDefault(x => x.Email == "lang@mail.com");
        Assert.NotNull(user);
        Assert.Equal("uk", user!.NativeLanguageCode);
        Assert.Equal("en", user.TargetLanguageCode);
    }

[Fact]
    public void Login_InvalidPassword_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        var emailSender = new FakeEmailSender();

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator(),
            new ForgotPasswordRequestValidator(),
            new ResetPasswordRequestValidator(),
            new VerifyEmailRequestValidator(),
            new ResendVerificationRequestValidator(),
            emailSender,
            new FakeOpenIdTokenValidator(),
            new FakeHostEnvironment(),
            new PasswordHasher()
        );

        service.Register(new RegisterRequest
        {
            Email = "test@mail.com",
            Password = "123456"
        });

        var user = dbContext.Users.First(x => x.Email == "test@mail.com");
        user.IsEmailVerified = true;
        dbContext.SaveChanges();

        Assert.Throws<UnauthorizedAccessException>(() =>
        {
            service.Login(new LoginRequest
            {
                Email = "test@mail.com",
                Password = "WRONG_PASSWORD"
            });
        });
    }
}
