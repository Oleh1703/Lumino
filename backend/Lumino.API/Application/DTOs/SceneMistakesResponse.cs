using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class SceneMistakesResponse
    {
        public int SceneId { get; set; }

        public int TotalMistakes { get; set; }

        public List<int> MistakeStepIds { get; set; } = new();

        public List<SceneStepResponse> Steps { get; set; } = new();
    }
}
