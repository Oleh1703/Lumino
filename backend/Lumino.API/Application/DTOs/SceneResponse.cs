namespace Lumino.Api.Application.DTOs
{
    public class SceneResponse
    {
        public int Id { get; set; }

        public int? CourseId { get; set; }

        public int Order { get; set; }

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string SceneType { get; set; } = null!;

        public string? BackgroundUrl { get; set; }

        public string? AudioUrl { get; set; }
    }
}
