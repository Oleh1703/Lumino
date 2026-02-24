using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface IResetPasswordRequestValidator
    {
        void Validate(ResetPasswordRequest request);
    }
}
