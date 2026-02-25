using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Validators;
using Xunit;

namespace Lumino.Tests;

public class DeleteAccountRequestValidatorTests
{
    [Fact]
    public void Validate_WithEmptyPassword_ShouldThrow()
    {
        var validator = new DeleteAccountRequestValidator();

        Assert.Throws<ArgumentException>(() =>
        {
            validator.Validate(new DeleteAccountRequest
            {
                Password = " "
            });
        });
    }

    [Fact]
    public void Validate_WithValidPassword_ShouldPass()
    {
        var validator = new DeleteAccountRequestValidator();

        validator.Validate(new DeleteAccountRequest
        {
            Password = "123456"
        });
    }
}
