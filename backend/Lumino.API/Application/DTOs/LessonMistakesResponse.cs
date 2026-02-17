using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class LessonMistakesResponse
    {
        public int LessonId { get; set; }

        public int TotalMistakes { get; set; }

        public List<int> MistakeExerciseIds { get; set; } = new();

        public List<ExerciseResponse> Exercises { get; set; } = new();
    }
}
