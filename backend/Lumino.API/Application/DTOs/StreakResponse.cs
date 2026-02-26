namespace Lumino.Api.Application.DTOs
{
    public class StreakResponse
    {
        public int Current { get; set; }

        public int Best { get; set; }

        public DateTime LastActivityDateUtc { get; set; }
    }
}
