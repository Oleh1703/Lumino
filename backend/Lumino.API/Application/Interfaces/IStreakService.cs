using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IStreakService
    {
        StreakResponse GetMyStreak(int userId);

        StreakCalendarResponse GetMyCalendar(int userId, int days);

        StreakCalendarResponse GetMyCalendarMonth(int userId, int year, int month);

        void RegisterLessonActivity(int userId);
    }
}
