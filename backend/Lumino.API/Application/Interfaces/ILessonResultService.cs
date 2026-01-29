using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface ILessonResultService
    {
        SubmitLessonResponse SubmitLesson(int userId, SubmitLessonRequest request);
    }
}
