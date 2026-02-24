namespace Lumino.Api.Utils
{
    public static class SupportedAvatars
    {
        // For now avatars are served by the frontend or static files under /avatars.
        // Backend stores AvatarUrl as a string and validates it against this list when provided.

        public static readonly string[] All = new[]
        {
            "/avatars/alien-1.png",
            "/avatars/alien-2.png",
            "/avatars/alien-3.png",
            "/avatars/alien-4.png",
            "/avatars/alien-5.png",
            "/avatars/alien-6.png"
        };

        public static string DefaultAvatarUrl => All[0];

        public static void Validate(string? avatarUrl, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return;
            }

            var value = avatarUrl.Trim();

            if (!All.Contains(value))
            {
                throw new ArgumentException($"{fieldName} is invalid");
            }
        }
    }
}
