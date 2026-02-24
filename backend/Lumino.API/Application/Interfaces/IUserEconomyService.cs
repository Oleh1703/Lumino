using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IUserEconomyService
    {
        void EnsureHasHeartsOrThrow(int userId);

        void RefreshHearts(int userId);

        void ConsumeHeartsForMistakes(int userId, int mistakesCount);

        void AwardCrystalsForPassedLessonIfNeeded(int userId);

        void AwardCrystalsForCompletedSceneIfNeeded(int userId);

        void AwardHeartForPracticeIfPossible(int userId);

        RestoreHeartsResponse RestoreHearts(int userId, RestoreHeartsRequest request);
    }
}
