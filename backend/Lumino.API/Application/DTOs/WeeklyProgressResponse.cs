namespace Lumino.Api.Application.DTOs
{
    public class WeeklyProgressDayResponse
    {
        public DateTime DateUtc { get; set; }

        public int Points { get; set; }
    }

    public class WeeklyProgressResponse
    {
        public List<WeeklyProgressDayResponse> CurrentWeek { get; set; } = new();

        public List<WeeklyProgressDayResponse> PreviousWeek { get; set; } = new();

        public int TotalPoints { get; set; }
    }
}
