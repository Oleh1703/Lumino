namespace Lumino.Api.Application.DTOs
{
    public class SceneDetailsResponse
    {
        public int Id { get; set; }

        public int? CourseId { get; set; }

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string SceneType { get; set; } = null!;

        public string? BackgroundUrl { get; set; }

        public string? AudioUrl { get; set; }

        public bool IsCompleted { get; set; }

        public bool IsUnlocked { get; set; }

        public int PassedLessons { get; set; }

        public int RequiredPassedLessons { get; set; }
    }
}
