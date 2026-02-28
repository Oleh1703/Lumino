namespace Lumino.Api.Application.DTOs
{
    public class StreakCalendarDayResponse
    {
        public DateTime DateUtc { get; set; }

        public bool IsActive { get; set; }
    }

    public class StreakCalendarResponse
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public List<StreakCalendarDayResponse> Days { get; set; } = new();
    }
}
