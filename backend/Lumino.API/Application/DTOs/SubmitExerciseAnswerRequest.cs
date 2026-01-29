namespace Lumino.Api.Application.DTOs
{
    public class SubmitExerciseAnswerRequest
    {
        public int ExerciseId { get; set; }

        public string Answer { get; set; } = null!;
    }
}
