using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public class VerifyEmailRequestValidator : IVerifyEmailRequestValidator
    {
        public void Validate(VerifyEmailRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new ArgumentException("Token is required");
            }

            if (request.Token.Trim().Length > 512)
            {
                throw new ArgumentException("Token is too long");
            }
        }
    }
}
