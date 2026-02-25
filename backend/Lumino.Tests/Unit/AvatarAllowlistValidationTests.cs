using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Validators;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lumino.Tests;

public class AvatarAllowlistValidationTests
{
    [Fact]
    public void Register_WithUnsupportedAvatar_ShouldThrow()
    {
        var validator = new RegisterRequestValidator(CreateConfig());

        Assert.Throws<ArgumentException>(() =>
        {
            validator.Validate(new RegisterRequest
            {
                Email = "test@mail.com",
                Password = "123456",
                Username = "testuser",
                AvatarUrl = "/avatars/not-exists.png"
            });
        });
    }

    [Fact]
    public void UpdateProfile_WithUnsupportedAvatar_ShouldThrow()
    {
        var validator = new UpdateProfileRequestValidator(CreateConfig());

        Assert.Throws<ArgumentException>(() =>
        {
            validator.Validate(new UpdateProfileRequest
            {
                AvatarUrl = "/avatars/not-exists.png"
            });
        });
    }

    [Fact]
    public void Register_WithAllowedAvatar_ShouldPass()
    {
        var validator = new RegisterRequestValidator(CreateConfig());

        validator.Validate(new RegisterRequest
        {
            Email = "test@mail.com",
            Password = "123456",
            Username = "testuser",
            AvatarUrl = "/avatars/alien-2.png"
        });
    }

    private static IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Avatars:Allowed:0"] = "/avatars/alien-1.png",
                ["Avatars:Allowed:1"] = "/avatars/alien-2.png"
            })
            .Build();
    }
}
