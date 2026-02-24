namespace Lumino.Api.Domain.Entities
{
    public class Course
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string LanguageCode { get; set; } = "en";

        public bool IsPublished { get; set; }
    }
}
