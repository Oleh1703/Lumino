namespace Lumino.Api.Application.DTOs
{
    public class AdminTopicResponse
    {
        public int Id { get; set; }

        public int CourseId { get; set; }

        public string Title { get; set; } = null!;

        public int Order { get; set; }
    }
}
