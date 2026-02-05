using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IAuthService
    {
        AuthResponse Register(RegisterRequest request);

        AuthResponse Login(LoginRequest request);

        AuthResponse Refresh(RefreshTokenRequest request);

        void Logout(RefreshTokenRequest request);
    }
}
