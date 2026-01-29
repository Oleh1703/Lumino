namespace Lumino.Api.Application.DTOs
{
    public class UserProgressResponse
    {
        public int CompletedLessons { get; set; }

        public int TotalScore { get; set; }

        public DateTime LastUpdatedAt { get; set; }
    }
}
