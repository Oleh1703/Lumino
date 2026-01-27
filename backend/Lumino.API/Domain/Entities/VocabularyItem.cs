namespace Lumino.Api.Domain.Entities
{
    public class VocabularyItem
    {
        public int Id { get; set; }

        public string Word { get; set; } = null!;

        public string Translation { get; set; } = null!;

        public string? Example { get; set; }
    }
}
