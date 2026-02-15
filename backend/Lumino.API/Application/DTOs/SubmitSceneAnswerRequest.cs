namespace Lumino.Api.Application.DTOs
{
    public class SubmitSceneAnswerRequest
    {
        public int StepId { get; set; }

        public string Answer { get; set; } = null!;
    }
}
