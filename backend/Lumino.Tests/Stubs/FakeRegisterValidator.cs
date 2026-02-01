using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Validators;

namespace Lumino.Tests;

public class FakeRegisterValidator : IRegisterRequestValidator
{
    public void Validate(RegisterRequest request) { }
}
