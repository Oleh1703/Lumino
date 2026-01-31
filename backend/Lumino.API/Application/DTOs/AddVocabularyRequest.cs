namespace Lumino.Api.Application.DTOs
{
    public class AddVocabularyRequest
    {
        public string Word { get; set; } = null!;

        public string Translation { get; set; } = null!;

        public string? Example { get; set; }
    }
}
