using Lumino.Api.Application.DTOs;
using Lumino.Api.Utils;

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

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                ValidateUsername(request.Username);
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

            SupportedAvatars.Validate(request.AvatarUrl, "AvatarUrl");

            var hasNative = !string.IsNullOrWhiteSpace(request.NativeLanguageCode);
            var hasTarget = !string.IsNullOrWhiteSpace(request.TargetLanguageCode);

            if (hasNative && !hasTarget)
            {
                throw new ArgumentException("TargetLanguageCode is required when NativeLanguageCode is provided");
            }

            // TargetLanguageCode can be provided without NativeLanguageCode.
            // In that case the application will use the default native language.
            if (hasTarget)
            {
                SupportedLanguages.ValidateLearnable(request.TargetLanguageCode, "TargetLanguageCode");

                if (hasNative)
                {
                    SupportedLanguages.ValidateNative(request.NativeLanguageCode, "NativeLanguageCode");

                    var native = SupportedLanguages.Normalize(request.NativeLanguageCode);
                    var target = SupportedLanguages.Normalize(request.TargetLanguageCode);

                    if (native == target)
                    {
                        throw new ArgumentException("NativeLanguageCode and TargetLanguageCode must be different");
                    }
                }
            }
        }

        private static void ValidateUsername(string username)
        {
            var value = username.Trim();

            if (value.Length < 3 || value.Length > 32)
            {
                throw new ArgumentException("Username length must be between 3 and 32 characters");
            }

            if (value.Contains(" "))
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
