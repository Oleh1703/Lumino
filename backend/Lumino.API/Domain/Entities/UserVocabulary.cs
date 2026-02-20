namespace Lumino.Api.Domain.Entities
{
    public class UserVocabulary
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int VocabularyItemId { get; set; }

        public DateTime AddedAt { get; set; }

        public DateTime? LastReviewedAt { get; set; }

        public DateTime NextReviewAt { get; set; }

        public int ReviewCount { get; set; }

        public string? ReviewIdempotencyKey { get; set; }
    }
}
