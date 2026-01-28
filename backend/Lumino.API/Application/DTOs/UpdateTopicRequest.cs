namespace Lumino.Api.Application.DTOs
{
    public class UpdateTopicRequest
    {
        public string Title { get; set; } = null!;

        public int Order { get; set; }
    }
}
