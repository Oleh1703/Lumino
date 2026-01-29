namespace Lumino.Api.Application.DTOs
{
    public class UpdateExerciseRequest
    {
        public string Type { get; set; } = null!;

        public string Question { get; set; } = null!;

        public string Data { get; set; } = null!;

        public string CorrectAnswer { get; set; } = null!;

        public int Order { get; set; }
    }
}
