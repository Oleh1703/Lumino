namespace Lumino.Api.Domain.Entities
{
    public class UserProgress
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CompletedLessons { get; set; }

        public int TotalScore { get; set; }

        public DateTime LastUpdatedAt { get; set; }
    }
}
