using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface IChangePasswordRequestValidator
    {
        void Validate(ChangePasswordRequest request);
    }
}
