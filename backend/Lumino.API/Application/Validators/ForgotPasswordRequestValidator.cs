using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public class ForgotPasswordRequestValidator : IForgotPasswordRequestValidator
    {
        public void Validate(ForgotPasswordRequest request)
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

            if (email.Length < 5 || email.Length > 100)
            {
                throw new ArgumentException("Email length must be between 5 and 100 characters");
            }

            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Email is invalid");
            }
        }

        private static bool IsValidEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0)
            {
                return false;
            }

            if (atIndex != email.LastIndexOf('@'))
            {
                return false;
            }

            if (atIndex >= email.Length - 1)
            {
                return false;
            }

            var domain = email.Substring(atIndex + 1);

            if (domain.Length < 3)
            {
                return false;
            }

            var dotIndex = domain.IndexOf('.');
            if (dotIndex <= 0 || dotIndex >= domain.Length - 1)
            {
                return false;
            }

            if (domain.Contains(" "))
            {
                return false;
            }

            return true;
        }
    }
}
