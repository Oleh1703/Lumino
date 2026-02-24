using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IUserAccountService
    {
        void ChangePassword(int userId, ChangePasswordRequest request);
    }
}
