namespace Lumino.Api.Application.DTOs
{
    public class AdminVocabularyItemResponse
    {
        public int Id { get; set; }

        public string Word { get; set; } = null!;

        public string? Example { get; set; }

        public List<string> Translations { get; set; } = new();

        public string? PartOfSpeech { get; set; }

        public string? Definition { get; set; }

        public List<string> Examples { get; set; } = new();

        public List<VocabularyRelationDto> Synonyms { get; set; } = new();

        public List<VocabularyRelationDto> Idioms { get; set; } = new();
    }
}
