namespace Lumino.Api.Utils
{
    public static class SceneUnlockRules
    {
        public static int NormalizeUnlockEveryLessons(int value)
        {
            return value < 1 ? 1 : value;
        }

        // scenePosition: використовуємо Scene.Order (якщо > 0), інакше fallback на Scene.Id
        public static int GetRequiredPassedLessons(int scenePosition, int unlockEveryLessons)
        {
            unlockEveryLessons = NormalizeUnlockEveryLessons(unlockEveryLessons);

            if (scenePosition <= 1)
            {
                return 0;
            }

            return (scenePosition - 1) * unlockEveryLessons;
        }

        public static bool IsUnlocked(int scenePosition, int passedLessonsCount, int unlockEveryLessons)
        {
            var required = GetRequiredPassedLessons(scenePosition, unlockEveryLessons);
            return passedLessonsCount >= required;
        }
    }
}
