using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface ICourseCompletionService
    {
        CourseCompletionResponse GetMyCourseCompletion(int userId, int courseId);
    }
}
