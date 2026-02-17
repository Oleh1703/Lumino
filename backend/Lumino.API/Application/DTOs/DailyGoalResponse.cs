namespace Lumino.Api.Application.DTOs
{
    public class DailyGoalResponse
    {
        public DateTime DateUtc { get; set; }

        public int TargetScore { get; set; }

        public int TodayScore { get; set; }

        public int RemainingScore { get; set; }

        public bool IsGoalMet { get; set; }

        public int TodayPassedLessons { get; set; }

        public int TodayCompletedScenes { get; set; }
    }
}
