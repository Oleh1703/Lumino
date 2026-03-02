using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IAuthService
    {
        AuthResponse Register(RegisterRequest request);

        AuthResponse Login(LoginRequest request);

        AuthResponse OAuthGoogle(OAuthLoginRequest request);

        ForgotPasswordResponse ForgotPassword(ForgotPasswordRequest request, string? ip, string? userAgent);

        void ResetPassword(ResetPasswordRequest request);

        AuthResponse VerifyEmail(VerifyEmailRequest request, string? ip, string? userAgent);

        ResendVerificationResponse ResendVerification(ResendVerificationRequest request, string? ip, string? userAgent);

        AuthResponse Refresh(RefreshTokenRequest request);

        void Logout(RefreshTokenRequest request);
    }
}
