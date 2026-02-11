using System;
using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class LessonResultDetailsResponse
    {
        public int ResultId { get; set; }

        public int LessonId { get; set; }

        public string LessonTitle { get; set; } = string.Empty;

        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        public bool IsPassed { get; set; }

        public DateTime CompletedAt { get; set; }

        public List<int> MistakeExerciseIds { get; set; } = new();

        public List<LessonAnswerResultDto> Answers { get; set; } = new();
    }
}
