namespace Lumino.Api.Application.DTOs
{
    public class OAuthLoginRequest
    {
        public string IdToken { get; set; } = null!;

        public string? Username { get; set; }

        public string? AvatarUrl { get; set; }
    }
}
