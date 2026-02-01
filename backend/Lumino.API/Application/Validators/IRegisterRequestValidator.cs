using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface IRegisterRequestValidator
    {
        void Validate(RegisterRequest request);
    }
}
