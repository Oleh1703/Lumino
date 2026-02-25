using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface IResendVerificationRequestValidator
    {
        void Validate(ResendVerificationRequest request);
    }
}
