namespace Lumino.Api.Application.DTOs
{
    public class VocabularyResponse
    {
        public int Id { get; set; }

        public int VocabularyItemId { get; set; }

        public string Word { get; set; } = null!;

        // основний переклад (для сумісності зі старими клієнтами)
        public string Translation { get; set; } = null!;

        // всі переклади (новий функціонал)
        public List<string> Translations { get; set; } = new();

        public string? Example { get; set; }

        public DateTime AddedAt { get; set; }

        public DateTime? LastReviewedAt { get; set; }

        public DateTime NextReviewAt { get; set; }

        public int ReviewCount { get; set; }
    }
}
