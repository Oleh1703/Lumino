namespace Lumino.Api.Application.Interfaces
{
    public interface IAchievementService
    {
        void CheckAndGrantAchievements(int userId, int lessonScore, int totalQuestions);

        void CheckAndGrantSceneAchievements(int userId);
    }
}
