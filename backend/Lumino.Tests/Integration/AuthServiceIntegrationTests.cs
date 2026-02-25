using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Utils;
using Lumino.Tests;
using Xunit;
using Lumino.Tests.Stubs;

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
            new RegisterRequestValidator(configuration),
            new LoginRequestValidator(),
	        new ForgotPasswordRequestValidator(),
	        new ResetPasswordRequestValidator(),
	        new FakeEmailSender(),
	        new FakeOpenIdTokenValidator(),
	        new FakeHostEnvironment(),
	        new PasswordHasher()
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
}
