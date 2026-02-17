using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface ILessonMistakesService
    {
        LessonMistakesResponse GetLessonMistakes(int userId, int lessonId);

        SubmitLessonMistakesResponse SubmitLessonMistakes(int userId, int lessonId, SubmitLessonMistakesRequest request);
    }
}
