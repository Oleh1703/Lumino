using System.Collections.Generic;

namespace Lumino.Api.Application.DTOs
{
    public class SubmitSceneResponse
    {
        public int SceneId { get; set; }

        public int TotalQuestions { get; set; }

        public int CorrectAnswers { get; set; }

        public bool IsCompleted { get; set; }

        public List<int> MistakeStepIds { get; set; } = new();

        public List<SceneStepAnswerResultDto> Answers { get; set; } = new();
    }
}
