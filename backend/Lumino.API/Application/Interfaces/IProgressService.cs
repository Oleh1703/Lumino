using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IProgressService
    {
        UserProgressResponse GetMyProgress(int userId);
        DailyGoalResponse GetMyDailyGoal(int userId);
    }
}
