using Lumino.Api.Domain.Enums;

namespace Lumino.Api.Domain.Entities
{
    public class Exercise
    {
        public int Id { get; set; }

        public int LessonId { get; set; }

        public ExerciseType Type { get; set; }

        public string Question { get; set; } = null!;

        public string Data { get; set; } = null!;

        public string CorrectAnswer { get; set; } = null!;

        public int Order { get; set; }
    }
}
