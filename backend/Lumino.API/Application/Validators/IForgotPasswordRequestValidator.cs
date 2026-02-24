using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface IForgotPasswordRequestValidator
    {
        void Validate(ForgotPasswordRequest request);
    }
}
