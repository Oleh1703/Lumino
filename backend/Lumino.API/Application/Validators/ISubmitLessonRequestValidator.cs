using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface ISubmitLessonRequestValidator
    {
        void Validate(SubmitLessonRequest request);
    }
}
