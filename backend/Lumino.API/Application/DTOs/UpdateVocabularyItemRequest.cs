namespace Lumino.Api.Application.DTOs
{
    public class UpdateVocabularyItemRequest
    {
        public string Word { get; set; } = null!;

        public List<string> Translations { get; set; } = new();

        public string? Example { get; set; }

        public string? PartOfSpeech { get; set; }

        public string? Definition { get; set; }

        public string? Transcription { get; set; }

        public string? Gender { get; set; }

        public List<string> Examples { get; set; } = new();

        public List<VocabularyRelationDto> Synonyms { get; set; } = new();

        public List<VocabularyRelationDto> Idioms { get; set; } = new();
    }
}
