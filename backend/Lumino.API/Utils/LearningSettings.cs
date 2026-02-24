using System.Collections.Generic;

namespace Lumino.Api.Utils
{
    public class LearningSettings
    {
        public int PassingScorePercent { get; set; } = 80;

        // щоденна ціль (як у Duolingo): скільки "очок" (правильних відповідей) треба набрати за день.
        public int DailyGoalScoreTarget { get; set; } = 20;

        public int SceneCompletionScore { get; set; } = 5;

        // поріг проходження сцени у відсотках (як у Duolingo). 100 = без помилок.
        public int ScenePassingPercent { get; set; } = 100;

        // скільки пройдених уроків потрібно на відкриття кожної наступної сцени.
        // правило: requiredLessons = (sceneId - 1) * SceneUnlockEveryLessons
        public int SceneUnlockEveryLessons { get; set; } = 1;

        // SRS (Vocabulary): через скільки годин повторюємо слово після помилки.
        public int VocabularyWrongDelayHours { get; set; } = 12;

        // SRS (Vocabulary): інтервали повторення в днях для правильних відповідей.
        // приклад: 1, 2, 4, 7, 14, 30, 60...
        public List<int> VocabularyReviewIntervalsDays { get; set; } = new List<int> { 1, 2, 4, 7, 14, 30, 60 };

        // Hearts / Crystals (Duolingo-like economy)
        public int HeartsMax { get; set; } = 5;

        // Скільки "сердечок" знімаємо за 1 помилку. 1 = класичний варіант.
        public int HeartsCostPerMistake { get; set; } = 1;

        // Скільки кристалів коштує відновити 1 сердечко.
        public int CrystalCostPerHeart { get; set; } = 10;

        // Автовідновлення: через скільки хвилин відновлюється 1 сердечко.
        public int HeartRegenMinutes { get; set; } = 30;

        // Нагорода кристалами за перше проходження уроку (passed).
        public int CrystalsRewardPerPassedLesson { get; set; } = 1;

        // Нагорода кристалами за перше проходження сцени (completed).
        public int CrystalsRewardPerCompletedScene { get; set; } = 1;
    }
}
