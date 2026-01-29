using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface IAdminLessonService
    {
        List<AdminLessonResponse> GetByTopic(int topicId);

        AdminLessonResponse Create(CreateLessonRequest request);

        void Update(int id, UpdateLessonRequest request);

        void Delete(int id);
    }
}
