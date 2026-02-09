using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class SceneService : ISceneService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        public SceneService(LuminoDbContext dbContext, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
        }

        public List<SceneResponse> GetAllScenes()
        {
            return _dbContext.Scenes
                .OrderBy(x => x.Id)
                .Select(x => new SceneResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    SceneType = x.SceneType
                })
                .ToList();
        }

        public void CreateScene(SceneResponse request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var scene = new Scene
            {
                Title = request.Title,
                Description = request.Description,
                SceneType = request.SceneType
            };

            _dbContext.Scenes.Add(scene);
            _dbContext.SaveChanges();
        }

        public void UpdateScene(int id, SceneResponse request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == id);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            scene.Title = request.Title;
            scene.Description = request.Description;
            scene.SceneType = request.SceneType;

            _dbContext.SaveChanges();
        }

        public void DeleteScene(int id)
        {
            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == id);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            _dbContext.Scenes.Remove(scene);
            _dbContext.SaveChanges();
        }

        public void MarkCompleted(int userId, int sceneId)
        {
            var exists = _dbContext.SceneAttempts
                .Any(x => x.UserId == userId && x.SceneId == sceneId);

            if (exists) return;

            _dbContext.SceneAttempts.Add(new SceneAttempt
            {
                UserId = userId,
                SceneId = sceneId,
                IsCompleted = true,
                CompletedAt = _dateTimeProvider.UtcNow
            });

            _dbContext.SaveChanges();
        }

        public List<int> GetCompletedScenes(int userId)
        {
            return _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .ToList();
        }
    }
}
