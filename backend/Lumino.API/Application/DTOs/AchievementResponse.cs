namespace Lumino.Api.Application.DTOs
{
    public class AchievementResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public DateTime EarnedAt { get; set; }
    }
}
