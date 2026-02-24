using Lumino.Api.Domain.Enums;

namespace Lumino.Api.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string? Username { get; set; }

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public Role Role { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? NativeLanguageCode { get; set; }

        public string? TargetLanguageCode { get; set; }

        public int Hearts { get; set; } = 5;

        public int Crystals { get; set; }

        public string Theme { get; set; } = "light";

        public List<RefreshToken> RefreshTokens { get; set; } = new();
    }
}
