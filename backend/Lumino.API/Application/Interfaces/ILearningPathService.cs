using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface ILearningPathService
    {
        LearningPathResponse GetMyCoursePath(int userId, int courseId);
    }
}
