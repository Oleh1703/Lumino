using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface IVocabularyService
    {
        List<VocabularyResponse> GetMyVocabulary(int userId);

        List<VocabularyResponse> GetDueVocabulary(int userId);

        VocabularyResponse? GetNextReview(int userId);

        void AddWord(int userId, AddVocabularyRequest request);

        VocabularyResponse ReviewWord(int userId, int userVocabularyId, ReviewVocabularyRequest request);

        void DeleteWord(int userId, int userVocabularyId);
    }
}
