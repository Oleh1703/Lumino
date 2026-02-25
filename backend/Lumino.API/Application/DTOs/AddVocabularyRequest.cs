namespace Lumino.Api.Application.DTOs
{
    public class AddVocabularyRequest
    {
        public string Word { get; set; } = null!;

        // основний переклад (для сумісності)
        public string Translation { get; set; } = null!;

        // додаткові переклади (опційно)
        public List<string>? Translations { get; set; }

        public string? Example { get; set; }
    }
}
