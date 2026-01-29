using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface IAdminExerciseService
    {
        List<AdminExerciseResponse> GetByLesson(int lessonId);

        AdminExerciseResponse Create(CreateExerciseRequest request);

        void Update(int id, UpdateExerciseRequest request);

        void Delete(int id);
    }
}
