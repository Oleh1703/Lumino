namespace Lumino.Api.Domain.Entities
{
    public class UserStreak
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CurrentStreak { get; set; }

        public int BestStreak { get; set; }

        public DateTime LastActivityDateUtc { get; set; }
    }
}
