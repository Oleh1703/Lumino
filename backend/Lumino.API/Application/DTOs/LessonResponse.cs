namespace Lumino.Api.Application.DTOs
{
    public class LessonResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Theory { get; set; } = null!;

        public int Order { get; set; }
    }
}
