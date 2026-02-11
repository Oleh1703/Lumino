namespace Lumino.Api.Application.DTOs
{
    public class UserLessonProgressResponse
    {
        public int LessonId { get; set; }

        public bool IsUnlocked { get; set; }

        public bool IsCompleted { get; set; }

        public int BestScore { get; set; }

        public DateTime? LastAttemptAt { get; set; }
    }
}
