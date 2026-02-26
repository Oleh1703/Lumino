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
            Days = new List<StreakCalendarDayResponse>()
        };
    }

    public void RegisterLessonActivity(int userId)
    {
    }
}
