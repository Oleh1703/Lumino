using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;

namespace Lumino.Api.Application.Services
{
    public class AdminSceneService : IAdminSceneService
    {
        private readonly LuminoDbContext _dbContext;

        public AdminSceneService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AdminSceneResponse> GetAll()
        {
            return _dbContext.Scenes
                .AsEnumerable()
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Select(x => new AdminSceneResponse
                {
                    Id = x.Id,
                    CourseId = x.CourseId,
                    Order = x.Order,
                    Title = x.Title,
                    Description = x.Description,
                    SceneType = x.SceneType,
                    BackgroundUrl = x.BackgroundUrl,
                    AudioUrl = x.AudioUrl
                })
                .ToList();
        }

        public AdminSceneDetailsResponse GetById(int id)
        {
            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == id);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var steps = GetStepsInternal(id);

            return new AdminSceneDetailsResponse
            {
                Id = scene.Id,
                CourseId = scene.CourseId,
                Order = scene.Order,
                Title = scene.Title,
                Description = scene.Description,
                SceneType = scene.SceneType,
                BackgroundUrl = scene.BackgroundUrl,
                AudioUrl = scene.AudioUrl,
                Steps = steps
            };
        }

        public AdminSceneDetailsResponse Create(CreateSceneRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            NormalizeStepsOrders(request.Steps);
            ValidateStepsOrders(request.Steps);

            var courseId = GetCourseIdOrDefault(request.CourseId);
            var sceneOrder = NormalizeOrder(request.Order);

            ValidateUniqueSceneOrder(courseId, sceneOrder, null);

            var scene = new Scene
            {
                Title = request.Title,
                Description = request.Description,
                SceneType = request.SceneType,
                BackgroundUrl = request.BackgroundUrl,
                CourseId = courseId,
                Order = sceneOrder,
                AudioUrl = request.AudioUrl
            };

            _dbContext.Scenes.Add(scene);
            _dbContext.SaveChanges();

            if (request.Steps != null && request.Steps.Count > 0)
            {
                var steps = request.Steps
                    .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                    .Select(x => new SceneStep
                    {
                        SceneId = scene.Id,
                        Order = x.Order,
                        Speaker = x.Speaker,
                        Text = x.Text,
                        StepType = x.StepType,
                        MediaUrl = x.MediaUrl,
                        ChoicesJson = x.ChoicesJson
                    })
                    .ToList();

                _dbContext.SceneSteps.AddRange(steps);
                _dbContext.SaveChanges();
            }

            return GetById(scene.Id);
        }

        public void Update(int id, UpdateSceneRequest request)
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

            var courseId = GetCourseIdOrDefault(request.CourseId);
            var sceneOrder = NormalizeOrder(request.Order);

            ValidateUniqueSceneOrder(courseId, sceneOrder, id);

            scene.Title = request.Title;
            scene.Description = request.Description;
            scene.CourseId = courseId;
            scene.Order = sceneOrder;
            scene.SceneType = request.SceneType;
            scene.BackgroundUrl = request.BackgroundUrl;
            scene.AudioUrl = request.AudioUrl;

            _dbContext.SaveChanges();
        }

        public void Delete(int id)
        {
            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == id);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var steps = _dbContext.SceneSteps.Where(x => x.SceneId == id).ToList();
            var attempts = _dbContext.SceneAttempts.Where(x => x.SceneId == id).ToList();

            if (steps.Count > 0)
            {
                _dbContext.SceneSteps.RemoveRange(steps);
            }

            if (attempts.Count > 0)
            {
                _dbContext.SceneAttempts.RemoveRange(attempts);
            }

            _dbContext.Scenes.Remove(scene);
            _dbContext.SaveChanges();
        }

        public List<AdminSceneStepResponse> GetSteps(int sceneId)
        {
            EnsureSceneExists(sceneId);

            return GetStepsInternal(sceneId);
        }

        public AdminSceneStepResponse AddStep(int sceneId, CreateSceneStepRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            EnsureSceneExists(sceneId);

            var stepOrder = NormalizeOrder(request.Order);

            bool orderExists = stepOrder > 0 && _dbContext.SceneSteps
                .Any(x => x.SceneId == sceneId && x.Order == stepOrder);

            if (orderExists)
            {
                throw new ArgumentException("Step with this Order already exists");
            }

            var step = new SceneStep
            {
                SceneId = sceneId,
                Order = stepOrder,
                Speaker = request.Speaker,
                Text = request.Text,
                StepType = request.StepType,
                MediaUrl = request.MediaUrl,
                ChoicesJson = request.ChoicesJson
            };

            _dbContext.SceneSteps.Add(step);
            _dbContext.SaveChanges();

            return new AdminSceneStepResponse
            {
                Id = step.Id,
                SceneId = step.SceneId,
                Order = step.Order,
                Speaker = step.Speaker,
                Text = step.Text,
                StepType = step.StepType,
                MediaUrl = step.MediaUrl,
                ChoicesJson = step.ChoicesJson
            };
        }

        public void UpdateStep(int sceneId, int stepId, UpdateSceneStepRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            EnsureSceneExists(sceneId);

            var step = _dbContext.SceneSteps.FirstOrDefault(x => x.Id == stepId && x.SceneId == sceneId);

            if (step == null)
            {
                throw new KeyNotFoundException("Scene step not found");
            }

            var stepOrder = NormalizeOrder(request.Order);

            bool orderExists = stepOrder > 0 && _dbContext.SceneSteps
                .Any(x => x.SceneId == sceneId && x.Order == stepOrder && x.Id != stepId);

            if (orderExists)
            {
                throw new ArgumentException("Step with this Order already exists");
            }

            step.Order = stepOrder;
            step.Speaker = request.Speaker;
            step.Text = request.Text;
            step.StepType = request.StepType;
            step.MediaUrl = request.MediaUrl;
            step.ChoicesJson = request.ChoicesJson;

            _dbContext.SaveChanges();
        }

        public void DeleteStep(int sceneId, int stepId)
        {
            EnsureSceneExists(sceneId);

            var step = _dbContext.SceneSteps.FirstOrDefault(x => x.Id == stepId && x.SceneId == sceneId);

            if (step == null)
            {
                throw new KeyNotFoundException("Scene step not found");
            }

            _dbContext.SceneSteps.Remove(step);
            _dbContext.SaveChanges();
        }

        private void EnsureSceneExists(int sceneId)
        {
            bool exists = _dbContext.Scenes.Any(x => x.Id == sceneId);

            if (!exists)
            {
                throw new KeyNotFoundException("Scene not found");
            }
        }

        private List<AdminSceneStepResponse> GetStepsInternal(int sceneId)
        {
            return _dbContext.SceneSteps
                .Where(x => x.SceneId == sceneId)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Select(x => new AdminSceneStepResponse
                {
                    Id = x.Id,
                    SceneId = x.SceneId,
                    Order = x.Order,
                    Speaker = x.Speaker,
                    Text = x.Text,
                    StepType = x.StepType,
                    MediaUrl = x.MediaUrl,
                    ChoicesJson = x.ChoicesJson
                })
                .ToList();
        }

        private int? GetCourseIdOrDefault(int? requestCourseId)
        {
            if (requestCourseId.HasValue && requestCourseId.Value > 0)
            {
                var exists = _dbContext.Courses.Any(x => x.Id == requestCourseId.Value);
                if (exists)
                {
                    return requestCourseId.Value;
                }
            }

            var published = _dbContext.Courses.FirstOrDefault(x => x.IsPublished);
            if (published != null)
            {
                return published.Id;
            }

            var any = _dbContext.Courses.FirstOrDefault();
            return any?.Id;
        }

        private void ValidateUniqueSceneOrder(int? courseId, int order, int? ignoreSceneId)
        {
            if (order <= 0)
            {
                return;
            }

            if (!courseId.HasValue || courseId.Value <= 0)
            {
                return;
            }

            bool exists = _dbContext.Scenes.Any(x => x.CourseId == courseId.Value && x.Order == order
                                               && (!ignoreSceneId.HasValue || x.Id != ignoreSceneId.Value));

            if (exists)
            {
                throw new ArgumentException("Scene with this Order already exists in this course");
            }
        }

        private void NormalizeStepsOrders(List<CreateSceneStepRequest> steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return;
            }

            foreach (var step in steps)
            {
                if (step.Order < 0)
                {
                    step.Order = 0;
                }
            }
        }

        private void ValidateStepsOrders(List<CreateSceneStepRequest> steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return;
            }

            // Дублі дозволені, якщо Order <= 0 (вони підуть у кінець і стабілізуються по Id)
            // Для Order > 0 — дублі заборонені
            var duplicateOrders = steps
                .Where(x => x.Order > 0)
                .GroupBy(x => x.Order)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateOrders.Count > 0)
            {
                throw new ArgumentException("Duplicate step Order values");
            }
        }

        private int NormalizeOrder(int order)
        {
            return order < 0 ? 0 : order;
        }
    }
}
