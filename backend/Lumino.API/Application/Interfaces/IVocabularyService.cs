using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface IVocabularyService
    {
        List<VocabularyResponse> GetMyVocabulary(int userId);

        void AddWord(int userId, AddVocabularyRequest request);

        void DeleteWord(int userId, int userVocabularyId);
    }
}
