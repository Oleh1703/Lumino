using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Xunit;

namespace Lumino.Tests;

public class AuthServiceTests
{
    [Fact]
    public void Register_ShouldCreateUser_AndReturnToken()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator()
        );

        var response = service.Register(new RegisterRequest
        {
            Email = "test@mail.com",
            Password = "123456"
        });

        Assert.False(string.IsNullOrWhiteSpace(response.Token));

        var user = dbContext.Users.FirstOrDefault(x => x.Email == "test@mail.com");
        Assert.NotNull(user);
        Assert.False(string.IsNullOrWhiteSpace(user!.PasswordHash));
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
            CreatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator()
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
    public void Login_InvalidPassword_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        var service = new AuthService(
            dbContext,
            configuration,
            new FakeRegisterValidator(),
            new FakeLoginValidator()
        );

        service.Register(new RegisterRequest
        {
            Email = "test@mail.com",
            Password = "123456"
        });

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
