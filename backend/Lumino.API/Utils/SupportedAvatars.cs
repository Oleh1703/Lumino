using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Lumino.Api.Utils
{
    public static class SupportedAvatars
    {
        // For now avatars are served by the backend static files under /avatars.
        // Backend stores AvatarUrl as a string and validates it against the allowlist.

        private static readonly string[] DefaultAll = new[]
        {
            "/avatars/alien-1.png",
            "/avatars/alien-2.png",
            "/avatars/alien-3.png",
            "/avatars/alien-4.png",
            "/avatars/alien-5.png",
            "/avatars/alien-6.png"
        };

        public static IReadOnlyList<string> GetAllowed(IConfiguration? configuration)
        {
            var configured = configuration
                ?.GetSection("Avatars:Allowed")
                .Get<string[]>();

            if (configured == null || configured.Length == 0)
            {
                return DefaultAll;
            }

            var normalized = configured
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToArray();

            if (normalized.Length == 0)
            {
                return DefaultAll;
            }

            return normalized;
        }

        public static string GetDefaultAvatarUrl(IConfiguration? configuration)
        {
            return GetAllowed(configuration)[0];
        }

        public static void Validate(string? avatarUrl, string fieldName, IConfiguration? configuration)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return;
            }

            var value = avatarUrl.Trim();
            var allowed = GetAllowed(configuration);

            if (!allowed.Contains(value))
            {
                throw new ArgumentException($"{fieldName} is invalid");
            }
        }
    }
}
