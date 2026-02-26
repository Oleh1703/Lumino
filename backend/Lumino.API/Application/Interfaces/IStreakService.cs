using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IStreakService
    {
        StreakResponse GetMyStreak(int userId);

        StreakCalendarResponse GetMyCalendar(int userId, int days);

        void RegisterLessonActivity(int userId);
    }
}
