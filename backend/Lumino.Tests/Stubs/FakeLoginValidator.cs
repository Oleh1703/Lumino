using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Validators;

namespace Lumino.Tests;

public class FakeLoginValidator : ILoginRequestValidator
{
    public void Validate(LoginRequest request) { }
}
