namespace Lumino.Api.Application.DTOs
{
    public class ExportSceneJson
    {
        public int? CourseId { get; set; }

        public int? TopicId { get; set; }

        public int Order { get; set; }

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string SceneType { get; set; } = null!;

        public string? BackgroundUrl { get; set; }

        public string? AudioUrl { get; set; }

        public List<ExportSceneStepJson> Steps { get; set; } = new();
    }

    public class ExportSceneStepJson
    {
        public int Order { get; set; }

        public string Speaker { get; set; } = null!;

        public string Text { get; set; } = null!;

        public string StepType { get; set; } = null!;

        public string? MediaUrl { get; set; }

        public string? ChoicesJson { get; set; }
    }
}
