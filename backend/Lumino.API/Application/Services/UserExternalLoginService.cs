using System;
using System.Collections.Generic;
using System.Linq;
using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class UserExternalLoginService : IUserExternalLoginService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IOpenIdTokenValidator _openIdTokenValidator;

        public UserExternalLoginService(LuminoDbContext dbContext, IOpenIdTokenValidator openIdTokenValidator)
        {
            _dbContext = dbContext;
            _openIdTokenValidator = openIdTokenValidator;
        }

        public List<ExternalLoginResponse> GetExternalLogins(int userId)
        {
            return _dbContext.UserExternalLogins
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new ExternalLoginResponse
                {
                    Provider = x.Provider,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToList();
        }

        public void UnlinkExternalLogin(int userId, UnlinkExternalLoginRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Provider))
            {
                throw new ArgumentException("Provider is required");
            }

            var provider = request.Provider.Trim().ToLowerInvariant();

            var external = _dbContext.UserExternalLogins
                .FirstOrDefault(x => x.UserId == userId && x.Provider == provider);

            if (external == null)
            {
                return;
            }

            var externalCount = _dbContext.UserExternalLogins.Count(x => x.UserId == userId);

            if (externalCount <= 1)
            {
                var user = _dbContext.Users.First(x => x.Id == userId);

                var hasUsedPasswordReset = _dbContext.PasswordResetTokens
                    .Any(x => x.UserId == userId && x.UsedAt != null);

                var isLikelyLinkedLater = external.CreatedAtUtc > user.CreatedAt.AddMinutes(2);

                if (!hasUsedPasswordReset && !isLikelyLinkedLater)
                {
                    throw new ForbiddenAccessException("Не можна відв’язати останній спосіб входу. Спочатку встановіть пароль через Forgot Password (скидання пароля).");
                }
            }

            _dbContext.UserExternalLogins.Remove(external);
            _dbContext.SaveChanges();
        }

        public void LinkGoogleExternalLogin(int userId, LinkExternalLoginRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                throw new ArgumentException("IdToken is required");
            }

            var info = _openIdTokenValidator.ValidateGoogleIdToken(request.IdToken.Trim());
            LinkExternalLoginInternal(userId, provider: "google", providerUserId: info.Subject, email: info.Email);
        }

        private void LinkExternalLoginInternal(int userId, string provider, string providerUserId, string? email)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                throw new ArgumentException("Provider is required");
            }

            if (string.IsNullOrWhiteSpace(providerUserId))
            {
                throw new ArgumentException("ProviderUserId is required");
            }

            var normalizedProvider = provider.Trim().ToLowerInvariant();
            var normalizedProviderUserId = providerUserId.Trim();

            var alreadyLinkedForUser = _dbContext.UserExternalLogins
                .FirstOrDefault(x => x.UserId == userId && x.Provider == normalizedProvider);

            if (alreadyLinkedForUser != null)
            {
                return;
            }

            var linkedToAnotherUser = _dbContext.UserExternalLogins
                .FirstOrDefault(x => x.Provider == normalizedProvider && x.ProviderUserId == normalizedProviderUserId);

            if (linkedToAnotherUser != null && linkedToAnotherUser.UserId != userId)
            {
                throw new ForbiddenAccessException("This external account is already linked to another user.");
            }

            var entity = new UserExternalLogin
            {
                UserId = userId,
                Provider = normalizedProvider,
                ProviderUserId = normalizedProviderUserId,
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.UserExternalLogins.Add(entity);
            _dbContext.SaveChanges();
        }
    }
}
