namespace Lumino.Api.Application.DTOs
{
    public class StreakCalendarDayResponse
    {
        public DateTime DateUtc { get; set; }

        public bool IsActive { get; set; }
    }

    public class StreakCalendarResponse
    {
        public List<StreakCalendarDayResponse> Days { get; set; } = new();
    }
}
