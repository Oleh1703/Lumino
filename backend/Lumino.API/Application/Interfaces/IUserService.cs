using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IUserService
    {
        UserProfileResponse GetCurrentUser(int userId);

        UserProfileResponse UpdateProfile(int userId, UpdateProfileRequest request);
    }
}
