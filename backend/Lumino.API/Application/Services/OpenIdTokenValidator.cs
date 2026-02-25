using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Lumino.Api.Application.Services
{
    public class OpenIdTokenValidator : IOpenIdTokenValidator
    {
        private readonly IConfiguration _configuration;

        private readonly ConfigurationManager<OpenIdConnectConfiguration> _googleConfigurationManager;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _appleConfigurationManager;

        public OpenIdTokenValidator(IConfiguration configuration)
        {
            _configuration = configuration;

            _googleConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://accounts.google.com/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever()
            );

            _appleConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                "https://appleid.apple.com/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever()
            );
        }

        public OpenIdUserInfo ValidateGoogleIdToken(string idToken)
        {
            var clientId = _configuration["OAuth:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("OAuth Google ClientId is not configured");
            }

            var config = _googleConfigurationManager.GetConfigurationAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[] { "https://accounts.google.com", "accounts.google.com" },
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var principal = handler.ValidateToken(idToken, parameters, out _);

            return BuildUserInfoFromPrincipal(principal, requireEmail: true);
        }

        public OpenIdUserInfo ValidateAppleIdToken(string idToken)
        {
            var clientId = _configuration["OAuth:Apple:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("OAuth Apple ClientId is not configured");
            }

            var config = _appleConfigurationManager.GetConfigurationAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[] { "https://appleid.apple.com", "appleid.apple.com" },
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var principal = handler.ValidateToken(idToken, parameters, out _);

            return BuildUserInfoFromPrincipal(principal, requireEmail: false);
        }

        private static OpenIdUserInfo BuildUserInfoFromPrincipal(ClaimsPrincipal principal, bool requireEmail)
        {
            var subject = principal.FindFirstValue("sub");
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new UnauthorizedAccessException("Invalid id_token: sub claim is missing");
            }

            if (requireEmail && string.IsNullOrWhiteSpace(email))
            {
                throw new UnauthorizedAccessException("Invalid id_token: email claim is missing");
            }

            var name = principal.FindFirstValue("name");
            var picture = principal.FindFirstValue("picture");

            return new OpenIdUserInfo
            {
                Subject = subject,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                Name = name,
                PictureUrl = picture
            };
        }
    }
}
