namespace Lumino.Api.Application.DTOs
{
    public class LoginRequest
    {
        public string? Login { get; set; }

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}
