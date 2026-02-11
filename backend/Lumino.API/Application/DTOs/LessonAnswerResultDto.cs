namespace Lumino.Api.Application.DTOs
{
    public class LessonAnswerResultDto
    {
        public int ExerciseId { get; set; }

        public string UserAnswer { get; set; } = string.Empty;

        public string CorrectAnswer { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}
