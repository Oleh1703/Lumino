using System;

namespace Lumino.Api.Utils
{
    public static class HeartsEconomyCalculator
    {
        public static int GetHeartsMax(LearningSettings learningSettings)
        {
            if (learningSettings == null)
            {
                return 5;
            }

            return learningSettings.HeartsMax <= 0 ? 5 : learningSettings.HeartsMax;
        }

        public static int GetHeartRegenMinutes(LearningSettings learningSettings)
        {
            if (learningSettings == null)
            {
                return 30;
            }

            return learningSettings.HeartRegenMinutes <= 0 ? 30 : learningSettings.HeartRegenMinutes;
        }

        public static int GetCrystalCostPerHeart(LearningSettings learningSettings)
        {
            if (learningSettings == null)
            {
                return 10;
            }

            return learningSettings.CrystalCostPerHeart <= 0 ? 10 : learningSettings.CrystalCostPerHeart;
        }

        public static DateTime? GetNextHeartAtUtc(int hearts, DateTime? heartsUpdatedAtUtc, LearningSettings learningSettings)
        {
            var heartsMax = GetHeartsMax(learningSettings);
            var regenMinutes = GetHeartRegenMinutes(learningSettings);

            if (regenMinutes <= 0)
            {
                return null;
            }

            if (hearts >= heartsMax)
            {
                return null;
            }

            var baseTime = heartsUpdatedAtUtc ?? DateTime.UtcNow;
            var next = baseTime.AddMinutes(regenMinutes);
            var nowUtc = DateTime.UtcNow;

            if (next < nowUtc)
            {
                next = nowUtc;
            }

            return next;
        }

        public static int GetNextHeartInSeconds(int hearts, DateTime? heartsUpdatedAtUtc, LearningSettings learningSettings)
        {
            var next = GetNextHeartAtUtc(hearts, heartsUpdatedAtUtc, learningSettings);

            if (next == null)
            {
                return 0;
            }

            var diff = next.Value - DateTime.UtcNow;

            if (diff.TotalSeconds <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(diff.TotalSeconds);
        }
    }
}
