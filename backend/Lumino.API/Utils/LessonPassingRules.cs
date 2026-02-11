namespace Lumino.Api.Utils
{
    public static class LessonPassingRules
    {
        public static int NormalizePassingPercent(int passingScorePercent)
        {
            if (passingScorePercent < 0) return 0;
            if (passingScorePercent > 100) return 100;

            return passingScorePercent;
        }

        public static bool IsPassed(int score, int totalQuestions, int passingScorePercent)
        {
            if (totalQuestions <= 0)
            {
                return false;
            }

            passingScorePercent = NormalizePassingPercent(passingScorePercent);

            if (passingScorePercent == 0)
            {
                return true;
            }

            return score * 100 >= totalQuestions * passingScorePercent;
        }
    }
}
