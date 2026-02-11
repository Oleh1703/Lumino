using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class LessonResultDetailsJson
    {
        public List<int> MistakeExerciseIds { get; set; } = new();

        public List<LessonAnswerResultDto> Answers { get; set; } = new();
    }
}
