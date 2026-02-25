using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public class DeleteAccountRequestValidator : IDeleteAccountRequestValidator
    {
        public void Validate(DeleteAccountRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required");
            }

            if (request.Password.Length > 64)
            {
                throw new ArgumentException("Password must be at most 64 characters");
            }
        }
    }
}
