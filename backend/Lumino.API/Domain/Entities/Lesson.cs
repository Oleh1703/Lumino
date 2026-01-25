namespace Lumino.Api.Domain.Entities
{
    public class Lesson
    {
        public int Id { get; set; }

        public int TopicId { get; set; }

        public string Title { get; set; } = null!;

        public string Theory { get; set; } = null!;

        public int Order { get; set; }
    }
}
