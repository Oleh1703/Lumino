using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface ISceneService
    {
        List<SceneResponse> GetAllScenes();

        SceneDetailsResponse GetSceneDetails(int userId, int sceneId);

        SceneContentResponse GetSceneContent(int userId, int sceneId);

        void CreateScene(SceneResponse request);

        void UpdateScene(int id, SceneResponse request);

        void DeleteScene(int id);

        void MarkCompleted(int userId, int sceneId);

        List<int> GetCompletedScenes(int userId);
    }
}
