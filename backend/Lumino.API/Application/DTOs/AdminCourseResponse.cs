namespace Lumino.Api.Application.DTOs
{
    public class AdminCourseResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public bool IsPublished { get; set; }
    }
}
