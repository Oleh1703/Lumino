namespace Lumino.Api.Application.DTOs
{
    public class LessonResultResponse
    {
        public int LessonId { get; set; }

        public string LessonTitle { get; set; } = null!;

        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        public DateTime CompletedAt { get; set; }
    }
}
