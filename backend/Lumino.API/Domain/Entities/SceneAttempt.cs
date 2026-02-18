namespace Lumino.Api.Domain.Entities
{
    public class SceneAttempt
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int SceneId { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CompletedAt { get; set; }

        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        public string? IdempotencyKey { get; set; }

        public string? DetailsJson { get; set; }
    }
}
