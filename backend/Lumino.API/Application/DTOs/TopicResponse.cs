namespace Lumino.Api.Application.DTOs
{
    public class TopicResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public int Order { get; set; }
    }
}
