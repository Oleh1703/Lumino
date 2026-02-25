using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public interface IDeleteAccountRequestValidator
    {
        void Validate(DeleteAccountRequest request);
    }
}
