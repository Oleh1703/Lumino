namespace Lumino.Api.Application.DTOs
{
    public class VocabularyResponse
    {
        public int Id { get; set; }           

        public int VocabularyItemId { get; set; }   

        public string Word { get; set; } = null!;

        public string Translation { get; set; } = null!;

        public string? Example { get; set; }

        public DateTime AddedAt { get; set; }

        public DateTime? LastReviewedAt { get; set; }
    }
}
