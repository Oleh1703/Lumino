using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface ILoginRequestValidator
    {
        void Validate(LoginRequest request);
    }
}
