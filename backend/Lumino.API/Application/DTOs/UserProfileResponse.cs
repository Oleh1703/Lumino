namespace Lumino.Api.Application.DTOs
{
    public class UserProfileResponse
    {
        public int Id { get; set; }

        public string? Username { get; set; }

        public string? AvatarUrl { get; set; }

        public string Email { get; set; } = null!;

        public bool IsEmailVerified { get; set; }

        public string Role { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public string? NativeLanguageCode { get; set; }

        public string? TargetLanguageCode { get; set; }

        public int Hearts { get; set; }

        public int Crystals { get; set; }

        public string Theme { get; set; } = null!;
    }
}
