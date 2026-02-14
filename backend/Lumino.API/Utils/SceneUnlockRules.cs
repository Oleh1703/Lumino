namespace Lumino.Api.Utils
{
    public static class SceneUnlockRules
    {
        public static int NormalizeUnlockEveryLessons(int value)
        {
            return value < 1 ? 1 : value;
        }

        public static int GetRequiredPassedLessons(int sceneId, int unlockEveryLessons)
        {
            unlockEveryLessons = NormalizeUnlockEveryLessons(unlockEveryLessons);

            if (sceneId <= 1)
            {
                return 0;
            }

            return (sceneId - 1) * unlockEveryLessons;
        }

        public static bool IsUnlocked(int sceneId, int passedLessonsCount, int unlockEveryLessons)
        {
            var required = GetRequiredPassedLessons(sceneId, unlockEveryLessons);
            return passedLessonsCount >= required;
        }
    }
}
