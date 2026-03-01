namespace Lumino.Api.Domain.Entities
{
    public class Course
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string LanguageCode { get; set; } = "en";

        // Explicit meta for stable UI ordering/locking (do not rely on Title parsing)
        public string? Level { get; set; }

        public int Order { get; set; }

        public int? PrerequisiteCourseId { get; set; }

        public bool IsPublished { get; set; }
    }
}
