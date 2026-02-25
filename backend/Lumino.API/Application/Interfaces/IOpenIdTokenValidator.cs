using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IOpenIdTokenValidator
    {
        OpenIdUserInfo ValidateGoogleIdToken(string idToken);

        OpenIdUserInfo ValidateAppleIdToken(string idToken);
    }
}
