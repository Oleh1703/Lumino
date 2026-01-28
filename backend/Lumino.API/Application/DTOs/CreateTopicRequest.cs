namespace Lumino.Api.Application.DTOs
{
    public class CreateTopicRequest
    {
        public int CourseId { get; set; }

        public string Title { get; set; } = null!;

        public int Order { get; set; }
    }
}
