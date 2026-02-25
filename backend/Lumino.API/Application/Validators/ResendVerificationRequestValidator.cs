using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public class ResendVerificationRequestValidator : IResendVerificationRequestValidator
    {
        public void Validate(ResendVerificationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required");
            }

            var email = request.Email.Trim();

            if (email.Length > 256)
            {
                throw new ArgumentException("Email is too long");
            }

            if (!email.Contains('@') || !email.Contains('.'))
            {
                throw new ArgumentException("Invalid email");
            }
        }
    }
}
