using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Interfaces
{
    public interface ISceneService
    {
        List<SceneResponse> GetAllScenes();

        SceneDetailsResponse GetSceneDetails(int userId, int sceneId);

        SceneContentResponse GetSceneContent(int userId, int sceneId);

        void MarkCompleted(int userId, int sceneId);

        SubmitSceneResponse SubmitScene(int userId, int sceneId, SubmitSceneRequest request);

        List<int> GetCompletedScenes(int userId);
    }
}
