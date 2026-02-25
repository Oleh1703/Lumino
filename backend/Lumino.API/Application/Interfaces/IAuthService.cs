using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IAuthService
    {
        AuthResponse Register(RegisterRequest request);

        AuthResponse Login(LoginRequest request);

        AuthResponse OAuthGoogle(OAuthLoginRequest request);

        AuthResponse OAuthApple(OAuthLoginRequest request);

        ForgotPasswordResponse ForgotPassword(ForgotPasswordRequest request, string? ip, string? userAgent);

        void ResetPassword(ResetPasswordRequest request);

        AuthResponse Refresh(RefreshTokenRequest request);

        void Logout(RefreshTokenRequest request);
    }
}
