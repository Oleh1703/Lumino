using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface IVerifyEmailRequestValidator
    {
        void Validate(VerifyEmailRequest request);
    }
}
