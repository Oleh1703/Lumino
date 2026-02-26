namespace Lumino.Api.Domain.Entities
{
    public class UserDailyActivity
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateTime DateUtc { get; set; }
    }
}
