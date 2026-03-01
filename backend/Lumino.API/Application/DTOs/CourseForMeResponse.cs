namespace Lumino.Api.Application.DTOs
{
    public class CourseForMeResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string LanguageCode { get; set; } = "en";

        public string? Level { get; set; }

        public int Order { get; set; }

        public int? PrerequisiteCourseId { get; set; }

        public bool IsLocked { get; set; }

        public bool IsCompleted { get; set; }

        public int CompletionPercent { get; set; }
    }
}
