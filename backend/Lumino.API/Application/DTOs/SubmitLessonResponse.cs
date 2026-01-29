namespace Lumino.Api.Application.DTOs
{
    public class SubmitLessonResponse
    {
        public int TotalExercises { get; set; }

        public int CorrectAnswers { get; set; }

        public bool IsPassed { get; set; }
    }
}
