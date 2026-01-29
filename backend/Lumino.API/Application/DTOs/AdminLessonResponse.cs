namespace Lumino.Api.Application.DTOs
{
    public class AdminLessonResponse
    {
        public int Id { get; set; }

        public int TopicId { get; set; }

        public string Title { get; set; } = null!;

        public string Theory { get; set; } = null!;

        public int Order { get; set; }
    }
}
