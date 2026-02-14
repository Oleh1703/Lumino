namespace Lumino.Api.Utils
{
    public class LearningSettings
    {
        public int PassingScorePercent { get; set; } = 80;

        public int SceneCompletionScore { get; set; } = 5;

        // скільки пройдених уроків потрібно на відкриття кожної наступної сцени.
        // правило: requiredLessons = (sceneId - 1) * SceneUnlockEveryLessons
        public int SceneUnlockEveryLessons { get; set; } = 1;
    }
}
