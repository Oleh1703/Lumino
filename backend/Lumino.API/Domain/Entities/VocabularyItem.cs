namespace Lumino.Api.Domain.Entities
{
    public class VocabularyItem
    {
        public int Id { get; set; }

        public string Word { get; set; } = null!;

        public string Translation { get; set; } = null!;

        public string? Example { get; set; }

        public string? PartOfSpeech { get; set; }

        public string? Definition { get; set; }

        public string? Transcription { get; set; }

        public string? Gender { get; set; }

        public string? ExamplesJson { get; set; }

        public string? SynonymsJson { get; set; }

        public string? IdiomsJson { get; set; }
    }
}
