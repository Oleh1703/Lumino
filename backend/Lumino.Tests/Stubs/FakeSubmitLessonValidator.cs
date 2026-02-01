using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Validators;

namespace Lumino.Tests;

public class FakeSubmitLessonValidator : ISubmitLessonRequestValidator
{
    public void Validate(SubmitLessonRequest request) { }
}