namespace Lumino.Api.Application.DTOs
{
    public class UpdateCourseRequest
    {
        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string? LanguageCode { get; set; }

        public string? Level { get; set; }

        public int Order { get; set; }

        public int? PrerequisiteCourseId { get; set; }

        public bool IsPublished { get; set; }
    }
}
