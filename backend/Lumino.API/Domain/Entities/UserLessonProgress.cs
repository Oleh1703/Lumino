namespace Lumino.Api.Domain.Entities
{
    public class UserLessonProgress
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int LessonId { get; set; }

        public bool IsUnlocked { get; set; }

        public bool IsCompleted { get; set; }

        public int BestScore { get; set; }

        public DateTime? LastAttemptAt { get; set; }
    }
}
