using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface IAdminSceneService
    {
        List<AdminSceneResponse> GetAll();

        AdminSceneDetailsResponse GetById(int id);

        AdminSceneDetailsResponse Create(CreateSceneRequest request);

        void Update(int id, UpdateSceneRequest request);

        void Delete(int id);

        List<AdminSceneStepResponse> GetSteps(int sceneId);

        AdminSceneStepResponse AddStep(int sceneId, CreateSceneStepRequest request);

        void UpdateStep(int sceneId, int stepId, UpdateSceneStepRequest request);

        void DeleteStep(int sceneId, int stepId);
    }
}
