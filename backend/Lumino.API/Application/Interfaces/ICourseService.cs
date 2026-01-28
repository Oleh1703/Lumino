using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface ICourseService
    {
        List<CourseResponse> GetPublishedCourses();
    }
}
