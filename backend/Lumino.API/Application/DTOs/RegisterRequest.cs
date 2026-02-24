namespace Lumino.Api.Application.DTOs
{
    public class RegisterRequest
    {
        public string? Username { get; set; }

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public string? NativeLanguageCode { get; set; }

        public string? TargetLanguageCode { get; set; }
    }
}
