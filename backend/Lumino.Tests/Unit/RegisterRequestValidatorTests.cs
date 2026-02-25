using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Validators;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lumino.Tests;

public class RegisterRequestValidatorTests
{
    
    private static IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Avatars:Allowed:0"] = "/avatars/alien-1.png"
            })
            .Build();
    }

[Fact]
    public void Validate_WithOnlyOneLanguageCode_ShouldThrow()
    {
        var validator = new RegisterRequestValidator(CreateConfig());
Assert.Throws<ArgumentException>(() =>
        {
            validator.Validate(new RegisterRequest
            {
                Email = "test@mail.com",
                Password = "123456",
                NativeLanguageCode = "en",
                TargetLanguageCode = null
            });
        });
    }

    [Fact]
    public void Validate_WithSameLanguageCodes_ShouldThrow()
    {
        var validator = new RegisterRequestValidator(CreateConfig());
Assert.Throws<ArgumentException>(() =>
        {
            validator.Validate(new RegisterRequest
            {
                Email = "test@mail.com",
                Password = "123456",
                NativeLanguageCode = "en",
                TargetLanguageCode = "en"
            });
        });
    }

    [Fact]
    public void Validate_WithUnsupportedLanguageCode_ShouldThrow()
    {
        var validator = new RegisterRequestValidator(CreateConfig());
Assert.Throws<ArgumentException>(() =>
        {
            validator.Validate(new RegisterRequest
            {
                Email = "test@mail.com",
                Password = "123456",
                NativeLanguageCode = "xx",
                TargetLanguageCode = "en"
            });
        });
    }

    [Fact]
    public void Validate_WithSupportedDifferentLanguages_ShouldPass()
    {
        var validator = new RegisterRequestValidator(CreateConfig());
validator.Validate(new RegisterRequest
        {
            Email = "test@mail.com",
            Password = "123456",
            NativeLanguageCode = "pl",
            TargetLanguageCode = "en"
        });
    }
}
