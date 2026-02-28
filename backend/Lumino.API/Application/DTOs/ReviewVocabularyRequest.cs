namespace Lumino.Api.Application.DTOs
{
    public class ReviewVocabularyRequest
    {
        // Backward compatible: if Action is null, IsCorrect is used.
        public bool IsCorrect { get; set; }

        // correct | wrong | skip (aka not_sure)
        public string? Action { get; set; }

        // захист від повторної відправки (double click / retry)
        public string? IdempotencyKey { get; set; }
    }
}
