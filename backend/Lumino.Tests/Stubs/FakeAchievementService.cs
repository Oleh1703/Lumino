using Lumino.Api.Application.Interfaces;

namespace Lumino.Tests;

public class FakeAchievementService : IAchievementService
{
    public void CheckAndGrantAchievements(int userId, int lessonScore, int totalQuestions) { }
}
