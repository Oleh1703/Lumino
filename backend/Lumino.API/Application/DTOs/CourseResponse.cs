namespace Lumino.Api.Application.DTOs
{
    public class CourseResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string LanguageCode { get; set; } = null!;

        public string? Level { get; set; }

        public int Order { get; set; }

        public int? PrerequisiteCourseId { get; set; }
    }
}
