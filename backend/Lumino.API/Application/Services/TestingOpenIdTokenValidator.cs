using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;

namespace Lumino.Api.Application.Services
{
    /// <summary>
    /// Тестовий валідатор OpenID токенів для середовища Testing.
    /// Не робить мережевих запитів і не перевіряє підпис — повертає стабільні дані
    /// для HTTP інтеграційних тестів.
    /// </summary>
    public class TestingOpenIdTokenValidator : IOpenIdTokenValidator
    {
        public OpenIdUserInfo ValidateGoogleIdToken(string idToken)
        {
            return ParseToken(idToken, providerPrefix: "test-google");
        }

        private OpenIdUserInfo ParseToken(string idToken, string providerPrefix)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new ArgumentException("IdToken is required");
            }

            // Формат:
            // 1) "test-google" => дефолтні дані
            // 2) "test-google:<subject>:<email>"
            var token = idToken.Trim();

            if (string.Equals(token, providerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return new OpenIdUserInfo
                {
                    Subject = providerPrefix + "-sub",
                    Email = providerPrefix + "@test.local"
                };
            }

            if (token.StartsWith(providerPrefix + ":", StringComparison.OrdinalIgnoreCase))
            {
                var parts = token.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 3)
                {
                    return new OpenIdUserInfo
                    {
                        Subject = parts[1],
                        Email = parts[2]
                    };
                }

                if (string.Equals(providerPrefix, "test-apple", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
                {
                    return new OpenIdUserInfo
                    {
                        Subject = parts[1],
                        Email = null
                    };
                }
            }

            throw new ArgumentException("Invalid testing IdToken format");
        }
    }
}
