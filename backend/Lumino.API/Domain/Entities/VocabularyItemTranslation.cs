namespace Lumino.Api.Domain.Entities
{
    public class VocabularyItemTranslation
    {
        public int Id { get; set; }

        public int VocabularyItemId { get; set; }

        public string Translation { get; set; } = null!;

        public int Order { get; set; }
    }
}
