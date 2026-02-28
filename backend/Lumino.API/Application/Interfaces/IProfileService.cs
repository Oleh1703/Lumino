using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IProfileService
    {
        WeeklyProgressResponse GetWeeklyProgress(int userId);
    }
}
