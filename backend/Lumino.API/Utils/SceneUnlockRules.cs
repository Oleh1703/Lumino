namespace Lumino.Api.Utils
{
    public static class SceneUnlockRules
    {
        public static int NormalizeUnlockEveryLessons(int value)
        {
            return value < 1 ? 1 : value;
        }

        // sceneOrderOrId: використовуємо Scene.Order (якщо > 0), інакше fallback на Scene.Id
        public static int GetRequiredPassedLessons(int sceneOrderOrId, int unlockEveryLessons)
        {
            unlockEveryLessons = NormalizeUnlockEveryLessons(unlockEveryLessons);

            if (sceneOrderOrId <= 1)
            {
                return 0;
            }

            return (sceneOrderOrId - 1) * unlockEveryLessons;
        }

        public static bool IsUnlocked(int sceneOrderOrId, int passedLessonsCount, int unlockEveryLessons)
        {
            var required = GetRequiredPassedLessons(sceneOrderOrId, unlockEveryLessons);
            return passedLessonsCount >= required;
        }
    }
}
