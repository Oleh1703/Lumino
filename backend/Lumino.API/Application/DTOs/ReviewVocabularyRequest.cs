namespace Lumino.Api.Application.DTOs
{
    public class ReviewVocabularyRequest
    {
        public bool IsCorrect { get; set; }

        // захист від повторної відправки (double click / retry)
        public string? IdempotencyKey { get; set; }
    }
}
