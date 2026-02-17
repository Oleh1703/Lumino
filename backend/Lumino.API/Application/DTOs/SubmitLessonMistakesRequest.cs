using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class SubmitLessonMistakesRequest
    {
        public List<SubmitExerciseAnswerRequest> Answers { get; set; } = new();
    }
}
