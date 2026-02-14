namespace Lumino.Api.Application.DTOs
{
    public class CreateSceneStepRequest
    {
        public int Order { get; set; }

        public string Speaker { get; set; } = null!;

        public string Text { get; set; } = null!;

        public string StepType { get; set; } = null!;

        public string? MediaUrl { get; set; }

        public string? ChoicesJson { get; set; }
    }
}
