namespace Lumino.Api.Application.DTOs
{
    public class ForgotPasswordResponse
    {
        public bool IsSent { get; set; }

        public string? ResetToken { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }
    }
}
