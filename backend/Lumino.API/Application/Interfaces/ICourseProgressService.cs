using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface ICourseProgressService
    {
        ActiveCourseResponse StartCourse(int userId, int courseId);

        ActiveCourseResponse? GetMyActiveCourse(int userId);

        List<UserLessonProgressResponse> GetMyLessonProgressByCourse(int userId, int courseId);
    }
}
