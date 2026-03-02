using System.Collections.Generic;
using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IUserExternalLoginService
    {
        List<ExternalLoginResponse> GetExternalLogins(int userId);

        void UnlinkExternalLogin(int userId, UnlinkExternalLoginRequest request);

        void LinkGoogleExternalLogin(int userId, LinkExternalLoginRequest request);
    }
}
