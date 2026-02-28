namespace Lumino.Api.Application.DTOs
{
    public class AddVocabularyRequest
    {
        public string Word { get; set; } = null!;

        // основний переклад (для сумісності)
        public string? Translation { get; set; }

        // додаткові переклади (опційно)
        public List<string>? Translations { get; set; }

        public string? Example { get; set; }

        public string? Transcription { get; set; }

        public string? Gender { get; set; }

        public List<string>? Examples { get; set; }
    }
}
