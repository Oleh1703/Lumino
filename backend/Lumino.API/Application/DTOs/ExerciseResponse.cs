namespace Lumino.Api.Application.DTOs
{
    public class ExerciseResponse
    {
        public int Id { get; set; }

        public string Type { get; set; } = null!;

        public string Question { get; set; } = null!;

        public string Data { get; set; } = null!;

        public int Order { get; set; }
    }
}
