using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class SubmitLessonMistakesRequest
    {
        // захист від повторної відправки (double click / retry)
        public string? IdempotencyKey { get; set; }

        public List<SubmitExerciseAnswerRequest> Answers { get; set; } = new();
    }
}
