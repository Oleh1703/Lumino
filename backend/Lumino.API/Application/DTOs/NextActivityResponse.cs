namespace Lumino.Api.Application.DTOs
{
    public class NextActivityResponse
    {
        public string Type { get; set; } = string.Empty;

        public int? CourseId { get; set; }

        public bool IsLocked { get; set; }

        public int? LessonId { get; set; }

        public int? TopicId { get; set; }

        public string? LessonTitle { get; set; }

        public int? SceneId { get; set; }

        public string? SceneTitle { get; set; }

        public int? UserVocabularyId { get; set; }

        public int? VocabularyItemId { get; set; }

        public string? Word { get; set; }

        public string? Translation { get; set; }
    }
}
