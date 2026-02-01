using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public class RegisterRequestValidator : IRegisterRequestValidator
    {
        public void Validate(RegisterRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required");
            }
        }
    }
}
