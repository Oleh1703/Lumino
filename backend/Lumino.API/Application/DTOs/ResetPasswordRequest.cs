namespace Lumino.Api.Application.DTOs
{
    public class ResetPasswordRequest
    {
        public string Token { get; set; } = null!;

        public string NewPassword { get; set; } = null!;

        public string ConfirmPassword { get; set; } = null!;
    }
}
