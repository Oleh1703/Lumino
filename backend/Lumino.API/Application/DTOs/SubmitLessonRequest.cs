using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class SubmitLessonRequest
    {
        public int LessonId { get; set; }

        // захист від повторної відправки (double click / retry)
        public string? IdempotencyKey { get; set; }

        public List<SubmitExerciseAnswerRequest> Answers { get; set; } = new();
    }
}
