using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Validators;

namespace Lumino.Tests;

public class FakeResetPasswordValidator : IResetPasswordRequestValidator
{
    public void Validate(ResetPasswordRequest request) { }
}
