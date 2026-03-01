namespace Lumino.Api.Application.DTOs
{
    public class RestoreHeartsResponse
    {
        public int Hearts { get; set; }

        public int Crystals { get; set; }

        public int HeartsMax { get; set; }

        public int HeartRegenMinutes { get; set; }

        public int CrystalCostPerHeart { get; set; }

        public DateTime? NextHeartAtUtc { get; set; }

        public int NextHeartInSeconds { get; set; }

        public int SpentCrystals { get; set; }

        public int RestoredHearts { get; set; }
    }
}
