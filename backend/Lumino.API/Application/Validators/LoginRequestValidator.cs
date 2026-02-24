using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public class LoginRequestValidator : ILoginRequestValidator
    {
        public void Validate(LoginRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var login = !string.IsNullOrWhiteSpace(request.Login)
                ? request.Login
                : request.Email;

            if (string.IsNullOrWhiteSpace(login))
            {
                throw new ArgumentException("Login is required");
            }

            var value = login.Trim();

            if (value.Contains('@'))
            {
                ValidateEmail(value);
            }
            else
            {
                ValidateUsername(value);
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required");
            }

            var password = request.Password;

            if (password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters");
            }

            if (password.Length > 64)
            {
                throw new ArgumentException("Password must be at most 64 characters");
            }
        }

        private static void ValidateEmail(string email)
        {
            if (email.Length < 5 || email.Length > 100)
            {
                throw new ArgumentException("Email length must be between 5 and 100 characters");
            }

            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Email is invalid");
            }
        }

        private static void ValidateUsername(string username)
        {
            if (username.Length < 3 || username.Length > 32)
            {
                throw new ArgumentException("Username length must be between 3 and 32 characters");
            }

            if (username.Contains(" "))
            {
                throw new ArgumentException("Username must not contain spaces");
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
