using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface IAdminCourseService
    {
        List<AdminCourseResponse> GetAll();

        AdminCourseResponse Create(CreateCourseRequest request);

        void Update(int id, UpdateCourseRequest request);

        void Delete(int id);
    }
}
