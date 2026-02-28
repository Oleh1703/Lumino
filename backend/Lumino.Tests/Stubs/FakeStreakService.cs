using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;

namespace Lumino.Tests;

public class FakeStreakService : IStreakService
{
    public StreakResponse GetMyStreak(int userId)
    {
        return new StreakResponse
        {
            Current = 0,
            Best = 0,
            LastActivityDateUtc = DateTime.UtcNow
        };
    }

    public StreakCalendarResponse GetMyCalendar(int userId, int days)
    {
        return new StreakCalendarResponse
        {
            Year = DateTime.UtcNow.Year,
            Month = DateTime.UtcNow.Month,
            Days = new List<StreakCalendarDayResponse>()
        };
    }


    public StreakCalendarResponse GetMyCalendarMonth(int userId, int year, int month)
    {
        return new StreakCalendarResponse
        {
            Year = year,
            Month = month,
            Days = new List<StreakCalendarDayResponse>()
        };
    }

    public void RegisterLessonActivity(int userId)
    {
    }
}
