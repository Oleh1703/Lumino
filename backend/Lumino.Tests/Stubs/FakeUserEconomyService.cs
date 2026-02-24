using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;

namespace Lumino.Tests;

public class FakeUserEconomyService : IUserEconomyService
{
    public int AwardHeartForPracticeCallsCount { get; private set; }

    public void EnsureHasHeartsOrThrow(int userId)
    {
    }

    public void RefreshHearts(int userId)
    {
    }

    public void ConsumeHeartsForMistakes(int userId, int mistakesCount)
    {
    }

    public void AwardCrystalsForPassedLessonIfNeeded(int userId)
    {
    }

    public void AwardCrystalsForCompletedSceneIfNeeded(int userId)
    {
    }

    public void AwardHeartForPracticeIfPossible(int userId)
    {
        AwardHeartForPracticeCallsCount++;
    }

    public RestoreHeartsResponse RestoreHearts(int userId, RestoreHeartsRequest request)
    {
        return new RestoreHeartsResponse
        {
            Hearts = 0,
            Crystals = 0
        };
    }
}
