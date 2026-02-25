namespace Lumino.Api.Application.DTOs
{
    public class AuthResponse
    {
        public string? Token { get; set; }

        public string? RefreshToken { get; set; }

        public bool RequiresEmailVerification { get; set; }
    }
}
