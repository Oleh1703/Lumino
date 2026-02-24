using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Validators;

namespace Lumino.Tests;

public class FakeForgotPasswordValidator : IForgotPasswordRequestValidator
{
    public void Validate(ForgotPasswordRequest request) { }
}
