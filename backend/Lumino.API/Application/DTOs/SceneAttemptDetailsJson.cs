using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class SceneAttemptDetailsJson
    {
        public List<int> MistakeStepIds { get; set; } = new();

        public List<SceneStepAnswerResultDto> Answers { get; set; } = new();
    }
}
