namespace Lumino.Api.Domain.Entities
{
    public class LessonResult
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int LessonId { get; set; }

        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        public DateTime CompletedAt { get; set; }
    }
}
