using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;

namespace Lumino.Api.Application.Services
{
    public class SceneService : ISceneService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAchievementService _achievementService;
        private readonly LearningSettings _learningSettings;

        public SceneService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IAchievementService achievementService,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _achievementService = achievementService;
            _learningSettings = learningSettings.Value;
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

            UpdateUserProgressAfterScene(userId);

            _achievementService.CheckAndGrantSceneAchievements(userId);
        }

        public List<int> GetCompletedScenes(int userId)
        {
            return _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .ToList();
        }

        private void UpdateUserProgressAfterScene(int userId)
        {
            var now = _dateTimeProvider.UtcNow;

            var progress = _dbContext.UserProgresses
                .FirstOrDefault(x => x.UserId == userId);

            int lessonsScore = _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .GroupBy(x => x.LessonId)
                .Select(g => g.Max(x => x.Score))
                .Sum();

            int completedDistinctScenes = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .Distinct()
                .Count();

            int scenesScore = completedDistinctScenes * _learningSettings.SceneCompletionScore;

            if (progress == null)
            {
                progress = new UserProgress
                {
                    UserId = userId,
                    CompletedLessons = 0,
                    TotalScore = lessonsScore + scenesScore,
                    LastUpdatedAt = now
                };

                _dbContext.UserProgresses.Add(progress);
                _dbContext.SaveChanges();
                return;
            }

            progress.TotalScore = lessonsScore + scenesScore;
            progress.LastUpdatedAt = now;

            _dbContext.SaveChanges();
        }
    }
}
