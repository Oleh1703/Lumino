using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IAdminUserService
    {
        List<AdminUserResponse> GetAll();
    }
}
