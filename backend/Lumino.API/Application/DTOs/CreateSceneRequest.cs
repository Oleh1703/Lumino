namespace Lumino.Api.Application.DTOs
{
    public class CreateSceneRequest
    {
        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string SceneType { get; set; } = null!;

        public string? BackgroundUrl { get; set; }

        public string? AudioUrl { get; set; }

        public List<CreateSceneStepRequest> Steps { get; set; } = new();
    }
}
