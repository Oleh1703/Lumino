namespace Lumino.Api.Domain.Entities
{
    public class Topic
    {
        public int Id { get; set; }

        public int CourseId { get; set; }

        public string Title { get; set; } = null!;

        public int Order { get; set; }
    }
}
