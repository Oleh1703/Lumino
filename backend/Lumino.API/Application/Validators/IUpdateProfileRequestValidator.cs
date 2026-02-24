using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface IUpdateProfileRequestValidator
    {
        void Validate(UpdateProfileRequest request);
    }
}
