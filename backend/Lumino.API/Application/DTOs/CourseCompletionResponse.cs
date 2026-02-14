using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class CourseCompletionResponse
    {
        public int CourseId { get; set; }

        public string Status { get; set; } = null!; // NotStarted / InProgress / Completed

        public int TotalLessons { get; set; }

        public int CompletedLessons { get; set; }

        public int CompletionPercent { get; set; }

        public int? NextLessonId { get; set; }

        public List<int> RemainingLessonIds { get; set; } = new List<int>();

        public bool ScenesIncluded { get; set; }
    }
}
