using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class SubmitLessonMistakesResponse
    {
        public int LessonId { get; set; }

        public int TotalExercises { get; set; }

        public int CorrectAnswers { get; set; }

        public bool IsCompleted { get; set; }

        public List<int> MistakeExerciseIds { get; set; } = new();

        public List<LessonAnswerResultDto> Answers { get; set; } = new();
    }
}
