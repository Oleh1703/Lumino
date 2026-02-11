using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface INextActivityService
    {
        NextActivityResponse? GetNext(int userId);
    }
}
