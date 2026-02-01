using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Xunit;

namespace Lumino.Tests.Integration;

public class AuthServiceIntegrationTests
{
    [Fact]
    public void Register_Then_Login_ShouldReturnToken()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        var service = new AuthService(
            dbContext,
            configuration,
            new RegisterRequestValidator(),
            new LoginRequestValidator()
        );

        var registerResponse = service.Register(new RegisterRequest
        {
            Email = "integration@mail.com",
            Password = "123456"
        });

        Assert.False(string.IsNullOrWhiteSpace(registerResponse.Token));

        var loginResponse = service.Login(new LoginRequest
        {
            Email = "integration@mail.com",
            Password = "123456"
        });

        Assert.False(string.IsNullOrWhiteSpace(loginResponse.Token));
    }

    [Fact]
    public void Register_EmptyEmail_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        var service = new AuthService(
            dbContext,
            configuration,
            new RegisterRequestValidator(),
            new LoginRequestValidator()
        );

        Assert.Throws<ArgumentException>(() =>
        {
            service.Register(new RegisterRequest
            {
                Email = "",
                Password = "123456"
            });
        });
    }

    [Fact]
    public void Login_UnknownEmail_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        var service = new AuthService(
            dbContext,
            configuration,
            new RegisterRequestValidator(),
            new LoginRequestValidator()
        );

        Assert.Throws<UnauthorizedAccessException>(() =>
        {
            service.Login(new LoginRequest
            {
                Email = "unknown@mail.com",
                Password = "123456"
            });
        });
    }
}
