using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;

namespace Lumino.Tests.Stubs
{
    public class FakeOpenIdTokenValidator : IOpenIdTokenValidator
    {
        public OpenIdUserInfo GoogleUserInfo { get; set; } = new OpenIdUserInfo
        {
            Subject = "google-subject",
            Email = "google@example.com",
            Name = "Google User",
            PictureUrl = null
        };

        public OpenIdUserInfo AppleUserInfo { get; set; } = new OpenIdUserInfo
        {
            Subject = "apple-subject",
            Email = "apple@example.com",
            Name = "Apple User",
            PictureUrl = null
        };

        public OpenIdUserInfo ValidateGoogleIdToken(string idToken)
        {
            return GoogleUserInfo;
        }

        public OpenIdUserInfo ValidateAppleIdToken(string idToken)
        {
            return AppleUserInfo;
        }
    }
}
