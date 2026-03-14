using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Lumino.Api.Application.Services
{
    public class SceneService : ISceneService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAchievementService _achievementService;
        private readonly IUserEconomyService _userEconomyService;
        private readonly ISubmitSceneRequestValidator _submitSceneRequestValidator;
        private readonly LearningSettings _learningSettings;

        public SceneService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IAchievementService achievementService,
            IUserEconomyService userEconomyService,
            IOptions<LearningSettings> learningSettings)
            : this(
                dbContext,
                dateTimeProvider,
                achievementService,
                userEconomyService,
                learningSettings,
                new SubmitSceneRequestValidator())
        {
        }

        public SceneService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IAchievementService achievementService,
            IUserEconomyService userEconomyService,
            IOptions<LearningSettings> learningSettings,
            ISubmitSceneRequestValidator submitSceneRequestValidator)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _achievementService = achievementService;
            _userEconomyService = userEconomyService;
            _learningSettings = learningSettings.Value;
            _submitSceneRequestValidator = submitSceneRequestValidator;
        }

        public List<SceneResponse> GetAllScenes()
        {
            return _dbContext.Scenes
                .AsEnumerable()
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Select(x => new SceneResponse
                {
                    Id = x.Id,
                    CourseId = x.CourseId,
                    TopicId = x.TopicId,
                    Order = x.Order,
                    Title = x.Title,
                    Description = x.Description,
                    SceneType = x.SceneType,
                    BackgroundUrl = x.BackgroundUrl,
                    AudioUrl = x.AudioUrl
                })
                .ToList();
        }


        public List<SceneDetailsResponse> GetAllSceneDetails(int userId, int? courseId)
        {
            var scenesQuery = _dbContext.Scenes
                .AsNoTracking()
                .AsQueryable();

            if (courseId != null)
            {
                scenesQuery = scenesQuery.Where(x => x.CourseId == courseId.Value);
            }

            var scenes = scenesQuery
                .AsEnumerable()
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            if (scenes.Count == 0)
            {
                return new List<SceneDetailsResponse>();
            }

            var courseIds = scenes
                .Where(x => x.CourseId != null)
                .Select(x => x.CourseId!.Value)
                .Distinct()
                .ToList();

            // passed lessons count per course
            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            var passedLessonsByCourse = new Dictionary<int, int>();
            foreach (var cid in courseIds)
            {
                passedLessonsByCourse[cid] = GetPassedDistinctLessonsCount(userId, cid);
            }

            // completed scenes
            var completedSceneIds = _dbContext.SceneAttempts
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .Distinct()
                .ToHashSet();

            // topic completion map (for topic-based unlock)
            var topicIds = scenes
                .Where(x => x.TopicId != null)
                .Select(x => x.TopicId!.Value)
                .Distinct()
                .ToList();

            var lessonsByTopic = _dbContext.Lessons
                .AsNoTracking()
                .Where(x => topicIds.Contains(x.TopicId))
                .Select(x => new { x.Id, x.TopicId })
                .ToList()
                .GroupBy(x => x.TopicId)
                .ToDictionary(x => x.Key, x => x.Select(v => v.Id).Distinct().ToList());

            var allLessonIds = lessonsByTopic
                .SelectMany(x => x.Value)
                .Distinct()
                .ToList();

            var passedLessonIds = _dbContext.LessonResults
                .AsNoTracking()
                .Where(x => x.UserId == userId && allLessonIds.Contains(x.LessonId) && x.TotalQuestions > 0)
                .Where(x => x.Score * 100 >= x.TotalQuestions * passingScorePercent)
                .Select(x => x.LessonId)
                .Distinct()
                .ToHashSet();

            var topicIsCompleted = new Dictionary<int, bool>();
            foreach (var kv in lessonsByTopic)
            {
                var ids = kv.Value;
                topicIsCompleted[kv.Key] = ids.Count == 0 || ids.All(x => passedLessonIds.Contains(x));
            }

            // build scene position per course scope (same logic as GetScenePosition but once)
            var positionBySceneId = new Dictionary<int, int>();

            foreach (var grp in scenes
                .GroupBy(x => x.CourseId))
            {
                var ordered = grp
                    .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                    .ThenBy(x => x.Id)
                    .Select(x => x.Id)
                    .ToList();

                for (int i = 0; i < ordered.Count; i++)
                {
                    positionBySceneId[ordered[i]] = i + 1;
                }
            }

            var result = new List<SceneDetailsResponse>();

            foreach (var scene in scenes)
            {
                var passedLessons = scene.CourseId != null && passedLessonsByCourse.ContainsKey(scene.CourseId.Value)
                    ? passedLessonsByCourse[scene.CourseId.Value]
                    : GetPassedDistinctLessonsCount(userId);

                var scenePosition = positionBySceneId.ContainsKey(scene.Id)
                    ? positionBySceneId[scene.Id]
                    : 1;

                var required = SceneUnlockRules.GetRequiredPassedLessons(scenePosition, _learningSettings.SceneUnlockEveryLessons);

                bool isUnlocked;

                if (scene.TopicId.HasValue)
                {
                    isUnlocked = topicIsCompleted.ContainsKey(scene.TopicId.Value)
                        ? topicIsCompleted[scene.TopicId.Value]
                        : GetIsSceneUnlockedByTopic(userId, scene.TopicId.Value);
                }
                else
                {
                    isUnlocked = SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons);
                }

                var unlockReason = GetSceneUnlockReason(scene, isUnlocked, required, passedLessons);

                result.Add(new SceneDetailsResponse
                {
                    Id = scene.Id,
                    CourseId = scene.CourseId,
                    TopicId = scene.TopicId,
                    Order = scene.Order,
                    Title = scene.Title,
                    Description = scene.Description,
                    SceneType = scene.SceneType,
                    BackgroundUrl = scene.BackgroundUrl,
                    AudioUrl = scene.AudioUrl,
                    IsCompleted = completedSceneIds.Contains(scene.Id),
                    IsUnlocked = isUnlocked,
                    UnlockReason = unlockReason,
                    PassedLessons = passedLessons,
                    RequiredPassedLessons = required
                });
            }

            return result;
        }

        public SceneDetailsResponse GetSceneDetails(int userId, int sceneId)
        {
            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == sceneId);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var passedLessons = GetPassedDistinctLessonsCount(userId, scene.CourseId);

            var scenePosition = GetScenePosition(scene);

            var required = SceneUnlockRules.GetRequiredPassedLessons(scenePosition, _learningSettings.SceneUnlockEveryLessons);

            var isUnlocked = scene.TopicId.HasValue
                ? GetIsSceneUnlockedByTopic(userId, scene.TopicId.Value)
                : SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            var unlockReason = GetSceneUnlockReason(scene, isUnlocked, required, passedLessons);
            var isCompleted = _dbContext.SceneAttempts
                            .Any(x => x.UserId == userId && x.SceneId == sceneId && x.IsCompleted);

            return new SceneDetailsResponse
            {
                Id = scene.Id,
                CourseId = scene.CourseId,
                TopicId = scene.TopicId,
                Order = scene.Order,
                Title = scene.Title,
                Description = scene.Description,
                SceneType = scene.SceneType,
                BackgroundUrl = scene.BackgroundUrl,
                AudioUrl = scene.AudioUrl,
                IsCompleted = isCompleted,
                IsUnlocked = isUnlocked,
                UnlockReason = unlockReason,
                PassedLessons = passedLessons,
                RequiredPassedLessons = required
            };
        }

        public SceneContentResponse GetSceneContent(int userId, int sceneId)
        {
            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == sceneId);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var passedLessons = GetPassedDistinctLessonsCount(userId, scene.CourseId);

            var scenePosition = GetScenePosition(scene);

            var required = SceneUnlockRules.GetRequiredPassedLessons(scenePosition, _learningSettings.SceneUnlockEveryLessons);

            var isUnlocked = scene.TopicId.HasValue
                ? GetIsSceneUnlockedByTopic(userId, scene.TopicId.Value)
                : SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            var unlockReason = GetSceneUnlockReason(scene, isUnlocked, required, passedLessons);

            var isCompleted = _dbContext.SceneAttempts
                .Any(x => x.UserId == userId && x.SceneId == sceneId && x.IsCompleted);

            var steps = new List<SceneStepResponse>();

            if (isUnlocked)
            {
                steps = _dbContext.SceneSteps
                    .Where(x => x.SceneId == sceneId)
                    .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                    .ThenBy(x => x.Id)
                    .Select(x => new SceneStepResponse
                    {
                        Id = x.Id,
                        Order = x.Order,
                        Speaker = x.Speaker,
                        Text = x.Text,
                        StepType = x.StepType,
                        MediaUrl = x.MediaUrl,
                        ChoicesJson = x.ChoicesJson
                    })
                    .ToList();
            }

            return new SceneContentResponse
            {
                Id = scene.Id,
                CourseId = scene.CourseId,
                TopicId = scene.TopicId,
                Order = scene.Order,
                Title = scene.Title,
                Description = scene.Description,
                SceneType = scene.SceneType,
                BackgroundUrl = scene.BackgroundUrl,
                AudioUrl = scene.AudioUrl,
                IsCompleted = isCompleted,
                IsUnlocked = isUnlocked,
                UnlockReason = unlockReason,
                PassedLessons = passedLessons,
                RequiredPassedLessons = required,
                Steps = steps
            };
        }

        public SceneMistakesResponse GetSceneMistakes(int userId, int sceneId)
        {
            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == sceneId);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var passedLessons = GetPassedDistinctLessonsCount(userId, scene.CourseId);
            var scenePosition = GetScenePosition(scene);

            var isUnlocked = scene.TopicId.HasValue
                ? GetIsSceneUnlockedByTopic(userId, scene.TopicId.Value)
                : SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            if (!isUnlocked)
            {
                throw new ForbiddenAccessException("Scene is locked");
            }

            var attempt = _dbContext.SceneAttempts
                .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

            if (attempt == null || string.IsNullOrWhiteSpace(attempt.DetailsJson))
            {
                return new SceneMistakesResponse
                {
                    SceneId = sceneId,
                    TotalMistakes = 0,
                    MistakeStepIds = new List<int>(),
                    Steps = new List<SceneStepResponse>()
                };
            }

            SceneAttemptDetailsJson? details;

            try
            {
                details = JsonSerializer.Deserialize<SceneAttemptDetailsJson>(attempt.DetailsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                details = null;
            }

            if (details == null || details.MistakeStepIds == null || details.MistakeStepIds.Count == 0)
            {
                return new SceneMistakesResponse
                {
                    SceneId = sceneId,
                    TotalMistakes = 0,
                    MistakeStepIds = new List<int>(),
                    Steps = new List<SceneStepResponse>()
                };
            }

            var mistakeIds = details.MistakeStepIds
                .Distinct()
                .ToList();

            var steps = _dbContext.SceneSteps
                .Where(x => mistakeIds.Contains(x.Id))
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Select(x => new SceneStepResponse
                {
                    Id = x.Id,
                    Order = x.Order,
                    Speaker = x.Speaker,
                    Text = x.Text,
                    StepType = x.StepType,
                    MediaUrl = x.MediaUrl,
                    ChoicesJson = x.ChoicesJson
                })
                .ToList();

            return new SceneMistakesResponse
            {
                SceneId = sceneId,
                TotalMistakes = mistakeIds.Count,
                MistakeStepIds = mistakeIds,
                Steps = steps
            };
        }

        public SubmitSceneResponse SubmitSceneMistakes(int userId, int sceneId, SubmitSceneRequest request)
        {
            _submitSceneRequestValidator.Validate(request);

            var mistakesIdempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);

            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == sceneId);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var passedLessons = GetPassedDistinctLessonsCount(userId, scene.CourseId);
            var scenePosition = GetScenePosition(scene);

            var isUnlocked = scene.TopicId.HasValue
                ? GetIsSceneUnlockedByTopic(userId, scene.TopicId.Value)
                : SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            if (!isUnlocked)
            {
                throw new ForbiddenAccessException("Scene is locked");
            }


            if (!string.IsNullOrWhiteSpace(mistakesIdempotencyKey))
            {
                var existingAttempt = _dbContext.SceneAttempts
                    .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

                if (existingAttempt != null
                    && !string.IsNullOrWhiteSpace(existingAttempt.MistakesIdempotencyKey)
                    && string.Equals(existingAttempt.MistakesIdempotencyKey, mistakesIdempotencyKey, StringComparison.Ordinal))
                {
                    return BuildSubmitSceneResponseFromAttempt(sceneId, existingAttempt);
                }
            }

            var steps = _dbContext.SceneSteps
                            .Where(x => x.SceneId == sceneId)
                            .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                            .ThenBy(x => x.Id)
                            .ToList();

            var questionSteps = steps
                .Where(x => !string.IsNullOrWhiteSpace(x.ChoicesJson))
                .ToList();

            int totalQuestions = questionSteps.Count;

            if (totalQuestions == 0)
            {
                // якщо питань немає — просто завершуємо сцену
                EnsureCompletedAttempt(userId, sceneId, score: 0, totalQuestions: 0, detailsJson: null, mistakesIdempotencyKey: mistakesIdempotencyKey);
                return new SubmitSceneResponse
                {
                    SceneId = sceneId,
                    TotalQuestions = 0,
                    CorrectAnswers = 0,
                    IsCompleted = true
                };
            }

            var attempt = _dbContext.SceneAttempts
                .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

            if (attempt == null || string.IsNullOrWhiteSpace(attempt.DetailsJson))
            {
                // якщо немає попередньої спроби — працюємо як звичайний submit
                return SubmitScene(userId, sceneId, request);
            }

            SceneAttemptDetailsJson? details;

            try
            {
                details = JsonSerializer.Deserialize<SceneAttemptDetailsJson>(attempt.DetailsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                details = null;
            }

            if (details == null)
            {
                return SubmitScene(userId, sceneId, request);
            }

            var allQuestionStepIds = questionSteps.Select(x => x.Id).ToHashSet();

            // беремо тільки ті помилки, які реально є question steps цієї сцени
            var targetMistakeStepIds = (details.MistakeStepIds ?? new List<int>())
                .Where(x => allQuestionStepIds.Contains(x))
                .Distinct()
                .ToList();

            // якщо помилок немає — повертаємо поточний стан спроби
            if (targetMistakeStepIds.Count == 0)
            {
                // гарантуємо, що Answers містять всі question steps
                EnsureDetailsContainsAllQuestionSteps(details, questionSteps);

                var correctCount = details.Answers.Count(x => x.IsCorrect);
                bool isCompleted = LessonPassingRules.IsPassed(correctCount, totalQuestions, _learningSettings.ScenePassingPercent);

                var detailsJsonNoChange = JsonSerializer.Serialize(details, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                EnsureCompletedAttempt(
                    userId,
                    sceneId,
                    score: correctCount,
                    totalQuestions: totalQuestions,
                    detailsJson: detailsJsonNoChange,
                    markCompleted: isCompleted,
                    mistakesIdempotencyKey: mistakesIdempotencyKey
                );

                return new SubmitSceneResponse
                {
                    SceneId = sceneId,
                    TotalQuestions = totalQuestions,
                    CorrectAnswers = correctCount,
                    IsCompleted = isCompleted,
                    MistakeStepIds = details.MistakeStepIds ?? new List<int>(),
                    Answers = details.Answers ?? new List<SceneStepAnswerResultDto>()
                };
            }

            var answersMap = new Dictionary<int, string>();

            foreach (var a in request.Answers)
            {
                if (!answersMap.ContainsKey(a.StepId))
                {
                    answersMap.Add(a.StepId, a.Answer);
                    continue;
                }

                throw new ArgumentException("Duplicate StepId in answers");
            }

            // швидкий доступ до старих результатів
            var existing = (details.Answers ?? new List<SceneStepAnswerResultDto>())
                .ToDictionary(x => x.StepId, x => x);

            foreach (var stepId in targetMistakeStepIds)
            {
                var step = questionSteps.First(x => x.Id == stepId);

                var correctAnswers = TryGetCorrectAnswersFromChoicesJson(step.StepType, step.ChoicesJson!);

                if (correctAnswers == null || correctAnswers.Count == 0)
                {
                    throw new ArgumentException($"Scene step {step.Id} has invalid ChoicesJson");
                }

                var correctAnswer = correctAnswers[0];

                answersMap.TryGetValue(step.Id, out string? newUserAnswer);

                // якщо нової відповіді не дали — залишаємо попередню
                if (string.IsNullOrWhiteSpace(newUserAnswer) && existing.TryGetValue(step.Id, out var prev))
                {
                    newUserAnswer = prev.UserAnswer;
                }

                newUserAnswer ??= string.Empty;

                bool isCorrect = correctAnswers.Any(x => IsAnswerCorrect(newUserAnswer, x));

                if (existing.TryGetValue(step.Id, out var dto))
                {
                    dto.UserAnswer = newUserAnswer;
                    dto.CorrectAnswer = correctAnswer;
                    dto.IsCorrect = isCorrect;
                }
                else
                {
                    existing[step.Id] = new SceneStepAnswerResultDto
                    {
                        StepId = step.Id,
                        UserAnswer = newUserAnswer,
                        CorrectAnswer = correctAnswer,
                        IsCorrect = isCorrect
                    };
                }
            }

            // синхронізуємо details.Answers з existing
            details.Answers = existing.Values
                .OrderBy(x => questionSteps.First(s => s.Id == x.StepId).Order)
                .ToList();

            // гарантуємо, що Answers містить всі question steps (інакше totalQuestions/correct можуть поїхати)
            EnsureDetailsContainsAllQuestionSteps(details, questionSteps);

            details.MistakeStepIds = details.Answers
                .Where(x => !x.IsCorrect)
                .Select(x => x.StepId)
                .Distinct()
                .ToList();

            var correct = details.Answers.Count(x => x.IsCorrect);
            bool completed = LessonPassingRules.IsPassed(correct, totalQuestions, _learningSettings.ScenePassingPercent);

            var detailsJson = JsonSerializer.Serialize(details, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            _userEconomyService.ConsumeHeartsForMistakes(userId, details.MistakeStepIds.Count);

            EnsureCompletedAttempt(
                userId,
                sceneId,
                score: correct,
                totalQuestions: totalQuestions,
                detailsJson: detailsJson,
                markCompleted: completed,
                mistakesIdempotencyKey: mistakesIdempotencyKey
            );

            if (completed && details.MistakeStepIds.Count == 0)
            {
                _userEconomyService.AwardHeartForPracticeIfPossible(userId);
            }

            return new SubmitSceneResponse
            {
                SceneId = sceneId,
                TotalQuestions = totalQuestions,
                CorrectAnswers = correct,
                IsCompleted = completed,
                MistakeStepIds = details.MistakeStepIds,
                Answers = details.Answers
            };
        }

        private static void EnsureDetailsContainsAllQuestionSteps(SceneAttemptDetailsJson details, List<SceneStep> questionSteps)
        {
            if (details.Answers == null)
            {
                details.Answers = new List<SceneStepAnswerResultDto>();
            }

            var byId = details.Answers.ToDictionary(x => x.StepId, x => x);

            foreach (var step in questionSteps)
            {
                if (byId.ContainsKey(step.Id)) continue;

                var correctAnswers = TryGetCorrectAnswersFromChoicesJson(step.StepType, step.ChoicesJson!);

                var correctAnswer = (correctAnswers != null && correctAnswers.Count > 0)
                    ? correctAnswers[0]
                    : string.Empty;

                details.Answers.Add(new SceneStepAnswerResultDto
                {
                    StepId = step.Id,
                    UserAnswer = string.Empty,
                    CorrectAnswer = correctAnswer,
                    IsCorrect = false
                });
            }

            // стабільний порядок як в сцені
            details.Answers = details.Answers
                .OrderBy(x => questionSteps.First(s => s.Id == x.StepId).Order)
                .ToList();
        }

        public SubmitSceneResponse SubmitScene(int userId, int sceneId, SubmitSceneRequest request)
        {
            _submitSceneRequestValidator.Validate(request);

            var submitIdempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);

            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == sceneId);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var passedLessons = GetPassedDistinctLessonsCount(userId, scene.CourseId);
            var scenePosition = GetScenePosition(scene);

            var isUnlocked = scene.TopicId.HasValue
                ? GetIsSceneUnlockedByTopic(userId, scene.TopicId.Value)
                : SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            if (!isUnlocked)
            {
                throw new ForbiddenAccessException("Scene is locked");
            }


            if (!string.IsNullOrWhiteSpace(submitIdempotencyKey))
            {
                var existingAttempt = _dbContext.SceneAttempts
                    .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

                if (existingAttempt != null
                    && !string.IsNullOrWhiteSpace(existingAttempt.SubmitIdempotencyKey)
                    && string.Equals(existingAttempt.SubmitIdempotencyKey, submitIdempotencyKey, StringComparison.Ordinal))
                {
                    return BuildSubmitSceneResponseFromAttempt(sceneId, existingAttempt);
                }
            }

            var steps = _dbContext.SceneSteps
                            .Where(x => x.SceneId == sceneId)
                            .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                            .ThenBy(x => x.Id)
                            .ToList();

            var questionSteps = steps
                .Where(x => !string.IsNullOrWhiteSpace(x.ChoicesJson))
                .ToList();

            int totalQuestions = questionSteps.Count;

            if (totalQuestions > 0)
            {
                _userEconomyService.EnsureHasHeartsOrThrow(userId);
            }

            // якщо в сцені немає choices (тільки діалоги/контент) — submit завершує сцену як “пройдено”
            if (totalQuestions == 0)
            {
                EnsureCompletedAttempt(userId, sceneId, score: 0, totalQuestions: 0, detailsJson: null, submitIdempotencyKey: submitIdempotencyKey);
                return new SubmitSceneResponse
                {
                    SceneId = sceneId,
                    TotalQuestions = 0,
                    CorrectAnswers = 0,
                    IsCompleted = true
                };
            }

            var answersMap = new Dictionary<int, string>();

            foreach (var a in request.Answers)
            {
                if (!answersMap.ContainsKey(a.StepId))
                {
                    answersMap.Add(a.StepId, a.Answer);
                    continue;
                }

                throw new ArgumentException("Duplicate StepId in answers");
            }

            var details = new SceneAttemptDetailsJson();
            int correct = 0;

            foreach (var step in questionSteps)
            {
                var correctAnswers = TryGetCorrectAnswersFromChoicesJson(step.StepType, step.ChoicesJson!);

                if (correctAnswers == null || correctAnswers.Count == 0)
                {
                    // якщо адмін поклав choicesJson без “правильної відповіді” — вважаємо крок некоректним
                    throw new ArgumentException($"Scene step {step.Id} has invalid ChoicesJson");
                }

                var correctAnswer = correctAnswers[0];

                answersMap.TryGetValue(step.Id, out string? userAnswer);
                userAnswer ??= string.Empty;

                bool isCorrect = correctAnswers.Any(x => IsAnswerCorrect(userAnswer, x));

                if (isCorrect)
                {
                    correct++;
                }
                else
                {
                    details.MistakeStepIds.Add(step.Id);
                }

                details.Answers.Add(new SceneStepAnswerResultDto
                {
                    StepId = step.Id,
                    UserAnswer = userAnswer,
                    CorrectAnswer = correctAnswer,
                    IsCorrect = isCorrect
                });
            }

            bool isCompleted = LessonPassingRules.IsPassed(correct, totalQuestions, _learningSettings.ScenePassingPercent);

            var detailsJson = JsonSerializer.Serialize(details, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            EnsureCompletedAttempt(
                userId,
                sceneId,
                score: correct,
                totalQuestions: totalQuestions,
                detailsJson: detailsJson,
                markCompleted: isCompleted,
                submitIdempotencyKey: submitIdempotencyKey
            );

            return new SubmitSceneResponse
            {
                SceneId = sceneId,
                TotalQuestions = totalQuestions,
                CorrectAnswers = correct,
                IsCompleted = isCompleted,
                MistakeStepIds = details.MistakeStepIds,
                Answers = details.Answers
            };
        }

        public void MarkCompleted(int userId, int sceneId)
        {
            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == sceneId);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var steps = _dbContext.SceneSteps
                .Where(x => x.SceneId == sceneId)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            var hasQuestions = steps
                .Any(x => !string.IsNullOrWhiteSpace(x.ChoicesJson)
                    || string.Equals(x.StepType, "Choice", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.StepType, "Input", StringComparison.OrdinalIgnoreCase));

            if (hasQuestions)
            {
                throw new ForbiddenAccessException("Scene has questions");
            }

            // заборона “закрити” сцену, якщо вона ще locked
            var passedLessons = GetPassedDistinctLessonsCount(userId, scene.CourseId);
            var scenePosition = GetScenePosition(scene);

            var isUnlocked = scene.TopicId.HasValue
                ? GetIsSceneUnlockedByTopic(userId, scene.TopicId.Value)
                : SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            if (!isUnlocked)
            {
                throw new ForbiddenAccessException("Scene is locked");
            }

            var attempt = _dbContext.SceneAttempts
                .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

            if (attempt != null)
            {
                if (attempt.IsCompleted) return;

                attempt.IsCompleted = true;
                attempt.CompletedAt = _dateTimeProvider.UtcNow;
                attempt.Score = 0;
                attempt.TotalQuestions = 0;
                attempt.DetailsJson = null;

                _dbContext.SaveChanges();

                AddSceneVocabularyIfNeeded(userId, sceneId, detailsJson: null);

                UpdateUserProgressAfterScene(userId);

                _achievementService.CheckAndGrantSceneAchievements(userId);

                return;
            }

            _dbContext.SceneAttempts.Add(new SceneAttempt
            {
                UserId = userId,
                SceneId = sceneId,
                IsCompleted = true,
                CompletedAt = _dateTimeProvider.UtcNow,
                Score = 0,
                TotalQuestions = 0,
                DetailsJson = null
            });

            try
            {
                _dbContext.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Захист від паралельних/повторних запитів:
                // якщо інший запит вже встиг створити SceneAttempt (унікальний індекс UserId+SceneId),
                // то перечитуємо існуючий запис і завершуємо його, замість падіння.
                var existingAttempt = _dbContext.SceneAttempts
                    .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

                if (existingAttempt == null)
                {
                    throw;
                }

                if (existingAttempt.IsCompleted)
                {
                    return;
                }

                existingAttempt.IsCompleted = true;
                existingAttempt.CompletedAt = _dateTimeProvider.UtcNow;
                existingAttempt.Score = 0;
                existingAttempt.TotalQuestions = 0;
                existingAttempt.DetailsJson = null;

                _dbContext.SaveChanges();
            }

            AddSceneVocabularyIfNeeded(userId, sceneId, detailsJson: null);

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

        private int GetPassedDistinctLessonsCount(int userId)
        {
            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            return _dbContext.LessonResults
                .Where(x => x.UserId == userId && x.TotalQuestions > 0)
                .Where(x => x.Score * 100 >= x.TotalQuestions * passingScorePercent)
                .Select(x => x.LessonId)
                .Distinct()
                .Count();
        }

        private int GetPassedDistinctLessonsCount(int userId, int? courseId)
        {
            if (courseId == null)
            {
                return GetPassedDistinctLessonsCount(userId);
            }

            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            var lessonIdsInCourse =
                (from t in _dbContext.Topics
                 join l in _dbContext.Lessons on t.Id equals l.TopicId
                 where t.CourseId == courseId.Value
                 select l.Id)
                .Distinct();

            return _dbContext.LessonResults
                .Where(x => x.UserId == userId && x.TotalQuestions > 0)
                .Where(x => lessonIdsInCourse.Contains(x.LessonId))
                .Where(x => x.Score * 100 >= x.TotalQuestions * passingScorePercent)
                .Select(x => x.LessonId)
                .Distinct()
                .Count();
        }

        private int GetScenePosition(Scene scene)
        {
            if (scene == null)
            {
                return 1;
            }

            var scenesQuery = _dbContext.Scenes.AsQueryable();

            if (scene.CourseId != null)
            {
                scenesQuery = scenesQuery.Where(x => x.CourseId == scene.CourseId.Value);
            }
            else
            {
                scenesQuery = scenesQuery.Where(x => x.CourseId == null);
            }

            var orderedIds = scenesQuery
                .AsEnumerable()
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            int index = orderedIds.IndexOf(scene.Id);

            if (index < 0)
            {
                return 1;
            }

            return index + 1;
        }


        private SubmitSceneResponse BuildSubmitSceneResponseFromAttempt(int sceneId, SceneAttempt attempt)
        {
            if (attempt == null)
            {
                return new SubmitSceneResponse
                {
                    SceneId = sceneId,
                    TotalQuestions = 0,
                    CorrectAnswers = 0,
                    IsCompleted = false
                };
            }

            var mistakeStepIds = new List<int>();
            var answers = new List<SceneStepAnswerResultDto>();

            if (!string.IsNullOrWhiteSpace(attempt.DetailsJson))
            {
                try
                {
                    var details = JsonSerializer.Deserialize<SceneAttemptDetailsJson>(attempt.DetailsJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (details != null)
                    {
                        if (details.MistakeStepIds != null)
                        {
                            mistakeStepIds = details.MistakeStepIds
                                .Distinct()
                                .ToList();
                        }

                        if (details.Answers != null)
                        {
                            answers = details.Answers
                                .ToList();
                        }
                    }
                }
                catch
                {
                    // ignore invalid json
                }
            }

            return new SubmitSceneResponse
            {
                SceneId = sceneId,
                TotalQuestions = attempt.TotalQuestions,
                CorrectAnswers = attempt.Score,
                IsCompleted = attempt.IsCompleted,
                MistakeStepIds = mistakeStepIds,
                Answers = answers
            };
        }

        private static string? NormalizeIdempotencyKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            var normalized = key.Trim();

            if (normalized.Length == 0)
            {
                return null;
            }

            if (normalized.Length > 64)
            {
                normalized = normalized.Substring(0, 64);
            }

            return normalized;
        }

        private static void ApplyAttemptIdempotencyKeys(SceneAttempt attempt, string? submitIdempotencyKey, string? mistakesIdempotencyKey)
        {
            if (!string.IsNullOrWhiteSpace(submitIdempotencyKey))
            {
                attempt.SubmitIdempotencyKey = submitIdempotencyKey;
            }

            if (!string.IsNullOrWhiteSpace(mistakesIdempotencyKey))
            {
                attempt.MistakesIdempotencyKey = mistakesIdempotencyKey;
            }

            var legacyKey = submitIdempotencyKey ?? mistakesIdempotencyKey;

            if (!string.IsNullOrWhiteSpace(legacyKey) && string.IsNullOrWhiteSpace(attempt.IdempotencyKey))
            {
                attempt.IdempotencyKey = legacyKey;
            }
        }

        private void EnsureCompletedAttempt(
            int userId,
            int sceneId,
            int score,
            int totalQuestions,
            string? detailsJson,
            bool markCompleted = true,
            string? submitIdempotencyKey = null,
            string? mistakesIdempotencyKey = null
        )
        {
            var now = _dateTimeProvider.UtcNow;

            var attempt = _dbContext.SceneAttempts
                .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

            // якщо вже пройдено — не перераховуємо прогрес/ачівки,
            // але дозволяємо оновити DetailsJson/Score (наприклад, щоб “доправити” помилки)
            if (attempt != null && attempt.IsCompleted)
            {
                attempt.Score = score;
                attempt.TotalQuestions = totalQuestions;
                attempt.DetailsJson = detailsJson;

                ApplyAttemptIdempotencyKeys(attempt, submitIdempotencyKey, mistakesIdempotencyKey);

                _dbContext.SaveChanges();
                return;
            }

            bool wasCompleted = attempt != null && attempt.IsCompleted;

            bool createdNewAttempt = false;

            if (attempt == null)
            {
                attempt = new SceneAttempt
                {
                    UserId = userId,
                    SceneId = sceneId,
                    IsCompleted = false,
                    CompletedAt = now,
                    Score = 0,
                    TotalQuestions = 0,
                    DetailsJson = null
                };

                _dbContext.SceneAttempts.Add(attempt);
                createdNewAttempt = true;
            }

            attempt.Score = score;
            attempt.TotalQuestions = totalQuestions;
            attempt.DetailsJson = detailsJson;

            ApplyAttemptIdempotencyKeys(attempt, submitIdempotencyKey, mistakesIdempotencyKey);

            // фіксуємо час останньої здачі (навіть якщо ще не completed)
            attempt.CompletedAt = now;

            if (markCompleted)
            {
                attempt.IsCompleted = true;
            }

            try
            {
                _dbContext.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Захист від паралельних/повторних запитів:
                // якщо інший запит вже встиг створити SceneAttempt (унікальний індекс UserId+SceneId),
                // то перечитуємо існуючий запис і оновлюємо його, замість падіння.
                if (!createdNewAttempt)
                {
                    throw;
                }

                attempt = _dbContext.SceneAttempts
                    .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

                if (attempt == null)
                {
                    throw;
                }

                wasCompleted = attempt.IsCompleted;

                attempt.Score = score;
                attempt.TotalQuestions = totalQuestions;
                attempt.DetailsJson = detailsJson;

                ApplyAttemptIdempotencyKeys(attempt, submitIdempotencyKey, mistakesIdempotencyKey);

                attempt.CompletedAt = now;

                if (markCompleted)
                {
                    attempt.IsCompleted = true;
                }

                _dbContext.SaveChanges();
            }

            if (markCompleted && !wasCompleted)
            {
                AddSceneVocabularyIfNeeded(userId, sceneId, detailsJson);

                UpdateUserProgressAfterScene(userId);

                UnlockNextTopicAfterSceneIfNeeded(userId, sceneId, now);
                TryMarkCourseCompletedAfterScene(userId, sceneId, now);

                _userEconomyService.AwardCrystalsForCompletedSceneIfNeeded(userId);

                _achievementService.CheckAndGrantSceneAchievements(userId);
            }
        }



        private void UnlockNextTopicAfterSceneIfNeeded(int userId, int sceneId, DateTime now)
        {
            var sceneInfo = _dbContext.Scenes
                .Where(x => x.Id == sceneId)
                .Select(x => new
                {
                    x.CourseId,
                    x.TopicId
                })
                .FirstOrDefault();

            if (sceneInfo == null || !sceneInfo.TopicId.HasValue || !sceneInfo.CourseId.HasValue)
            {
                return;
            }

            var orderedTopics = _dbContext.Topics
                .Where(x => x.CourseId == sceneInfo.CourseId.Value)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id
                })
                .ToList();

            int topicIndex = orderedTopics.FindIndex(x => x.Id == sceneInfo.TopicId.Value);

            if (topicIndex < 0 || topicIndex + 1 >= orderedTopics.Count)
            {
                return;
            }

            int nextTopicId = orderedTopics[topicIndex + 1].Id;

            var firstLessonId = _dbContext.Lessons
                .Where(x => x.TopicId == nextTopicId)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefault();

            if (firstLessonId == null)
            {
                return;
            }

            var progress = _dbContext.UserLessonProgresses
                .FirstOrDefault(x => x.UserId == userId && x.LessonId == firstLessonId.Value);

            if (progress == null)
            {
                progress = new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = firstLessonId.Value,
                    IsUnlocked = true,
                    IsCompleted = false,
                    BestScore = 0,
                    LastAttemptAt = now
                };

                _dbContext.UserLessonProgresses.Add(progress);
            }
            else if (!progress.IsUnlocked)
            {
                progress.IsUnlocked = true;

                if (progress.LastAttemptAt == null)
                {
                    progress.LastAttemptAt = now;
                }
            }

            var userCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.CourseId == sceneInfo.CourseId.Value);

            if (userCourse != null)
            {
                userCourse.LastLessonId = firstLessonId.Value;
                userCourse.LastOpenedAt = now;
            }

            _dbContext.SaveChanges();
        }

        private void TryMarkCourseCompletedAfterScene(int userId, int sceneId, DateTime now)
        {
            var courseId = _dbContext.Scenes
                .Where(x => x.Id == sceneId)
                .Select(x => (int?)x.CourseId)
                .FirstOrDefault();

            if (courseId == null)
            {
                return;
            }

            var userCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.CourseId == courseId.Value);

            if (userCourse == null || userCourse.IsCompleted)
            {
                return;
            }

            var lessonIds = (
                from t in _dbContext.Topics
                join l in _dbContext.Lessons on t.Id equals l.TopicId
                where t.CourseId == courseId.Value
                select l.Id)
                .Distinct()
                .ToList();

            if (lessonIds.Count == 0)
            {
                return;
            }

            var completedLessonIds = _dbContext.UserLessonProgresses
                .Where(x => x.UserId == userId && x.IsCompleted && lessonIds.Contains(x.LessonId))
                .Select(x => x.LessonId)
                .Distinct()
                .ToHashSet();

            if (completedLessonIds.Count < lessonIds.Count)
            {
                return;
            }

            var courseSceneIds = _dbContext.Scenes
                .Where(x => x.CourseId == courseId.Value)
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            if (courseSceneIds.Count > 0)
            {
                var completedSceneIds = _dbContext.SceneAttempts
                    .Where(x => x.UserId == userId && x.IsCompleted && courseSceneIds.Contains(x.SceneId))
                    .Select(x => x.SceneId)
                    .Distinct()
                    .ToHashSet();

                if (completedSceneIds.Count < courseSceneIds.Count)
                {
                    return;
                }
            }

            userCourse.IsCompleted = true;
            userCourse.IsActive = false;
            userCourse.CompletedAt = now;
            userCourse.LastLessonId = null;
            userCourse.LastOpenedAt = now;

            _dbContext.SaveChanges();
        }

        private void AddSceneVocabularyIfNeeded(int userId, int sceneId, string? detailsJson)
        {
            var now = _dateTimeProvider.UtcNow;

            var steps = _dbContext.SceneSteps
                .Where(x => x.SceneId == sceneId)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            if (steps.Count == 0)
            {
                return;
            }

            var mistakeStepIds = new HashSet<int>();

            if (!string.IsNullOrWhiteSpace(detailsJson))
            {
                try
                {
                    var details = JsonSerializer.Deserialize<SceneAttemptDetailsJson>(detailsJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (details != null && details.MistakeStepIds != null)
                    {
                        mistakeStepIds = details.MistakeStepIds
                            .Distinct()
                            .ToHashSet();
                    }
                }
                catch
                {
                    // ignore invalid json
                }
            }

            var allKeys = SceneVocabularyExtractor.ExtractVocabularyKeys(steps);

            if (allKeys.Count == 0)
            {
                return;
            }

            HashSet<string> mistakeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (mistakeStepIds.Count > 0)
            {
                var mistakeSteps = steps
                    .Where(x => mistakeStepIds.Contains(x.Id))
                    .ToList();

                mistakeKeys = SceneVocabularyExtractor.ExtractVocabularyKeys(mistakeSteps);
            }

            var vocabItems = _dbContext.VocabularyItems
                .AsEnumerable()
                .Where(x => !string.IsNullOrWhiteSpace(x.Word)
                    && allKeys.Contains(x.Word.Trim().ToLowerInvariant()))
                .ToList();

            if (vocabItems.Count == 0)
            {
                return;
            }

            var existing = _dbContext.UserVocabularies
                .Where(x => x.UserId == userId)
                .Select(x => x.VocabularyItemId)
                .ToHashSet();

            foreach (var item in vocabItems)
            {
                bool isMistake = mistakeKeys.Contains(item.Word.Trim().ToLowerInvariant());

                if (!existing.Contains(item.Id))
                {
                    _dbContext.UserVocabularies.Add(new UserVocabulary
                    {
                        UserId = userId,
                        VocabularyItemId = item.Id,
                        AddedAt = now,
                        LastReviewedAt = null,
                        NextReviewAt = isMistake ? now : now.AddDays(1),
                        ReviewCount = 0
                    });

                    continue;
                }

                if (!isMistake)
                {
                    continue;
                }

                var userWord = _dbContext.UserVocabularies
                    .FirstOrDefault(x => x.UserId == userId && x.VocabularyItemId == item.Id);

                if (userWord != null && userWord.NextReviewAt > now)
                {
                    userWord.NextReviewAt = now;
                }
            }

            _dbContext.SaveChanges();
        }


        private static bool IsAnswerCorrect(string userAnswer, string correctAnswer)
        {
            return NormalizeAnswer(userAnswer) == NormalizeAnswer(correctAnswer);
        }

        private static string NormalizeAnswer(string value)
        {
            return AnswerNormalizer.Normalize(value);
        }

        private static List<string>? TryGetCorrectAnswersFromChoicesJson(string stepType, string choicesJson)
        {
            if (string.IsNullOrWhiteSpace(choicesJson))
            {
                return null;
            }

            // строго підтримуємо 2 формати:
            // 1) Choice: [{"text":"...","isCorrect":true}, ...]
            // 2) Input: {"correctAnswer":"...","acceptableAnswers":["...","..."]}

            try
            {
                using var doc = JsonDocument.Parse(choicesJson);

                if (string.Equals(stepType, "Input", StringComparison.OrdinalIgnoreCase))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        return null;
                    }

                    var list = new List<string>();

                    var correctAnswer = TryGetString(doc.RootElement, "correctAnswer")
                        ?? TryGetString(doc.RootElement, "CorrectAnswer");

                    if (!string.IsNullOrWhiteSpace(correctAnswer))
                    {
                        list.Add(correctAnswer);
                    }

                    if (doc.RootElement.TryGetProperty("acceptableAnswers", out var acceptable)
                        && acceptable.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in acceptable.EnumerateArray())
                        {
                            if (item.ValueKind != JsonValueKind.String) continue;

                            var val = item.GetString();
                            if (string.IsNullOrWhiteSpace(val)) continue;

                            list.Add(val);
                        }
                    }

                    return list.Count > 0 ? list : null;
                }

                if (string.Equals(stepType, "Choice", StringComparison.OrdinalIgnoreCase))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    {
                        return null;
                    }

                    var list = new List<string>();

                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object) continue;

                        bool isCorrect = TryGetBool(item, "isCorrect")
                            || TryGetBool(item, "IsCorrect")
                            || TryGetBool(item, "correct")
                            || TryGetBool(item, "Correct");

                        if (!isCorrect) continue;

                        var text = TryGetString(item, "text")
                            ?? TryGetString(item, "Text")
                            ?? TryGetString(item, "value")
                            ?? TryGetString(item, "Value")
                            ?? TryGetString(item, "answer")
                            ?? TryGetString(item, "Answer");

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            list.Add(text);
                        }
                    }

                    return list.Count > 0 ? list : null;
                }

                // fallback: старий формат (щоб не зламати вже наявний контент)
                var one = TryGetCorrectAnswerFromChoices(choicesJson);

                if (!string.IsNullOrWhiteSpace(one))
                {
                    return new List<string> { one };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string? TryGetCorrectAnswerFromChoices(string choicesJson)
        {
            if (string.IsNullOrWhiteSpace(choicesJson))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(choicesJson);

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    string? firstString = null;

                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            if (firstString == null)
                            {
                                firstString = item.GetString();
                            }

                            continue;
                        }

                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            bool isCorrect = TryGetBool(item, "isCorrect")
                                || TryGetBool(item, "IsCorrect")
                                || TryGetBool(item, "correct")
                                || TryGetBool(item, "Correct");

                            if (isCorrect)
                            {
                                var text = TryGetString(item, "text")
                                    ?? TryGetString(item, "Text")
                                    ?? TryGetString(item, "value")
                                    ?? TryGetString(item, "Value")
                                    ?? TryGetString(item, "answer")
                                    ?? TryGetString(item, "Answer");

                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    return text;
                                }
                            }
                        }
                    }

                    // fallback: якщо choicesJson = ["A","B","C"] — беремо перший як “правильний”
                    return firstString;
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var correctAnswer = TryGetString(doc.RootElement, "correctAnswer")
                        ?? TryGetString(doc.RootElement, "CorrectAnswer");

                    if (!string.IsNullOrWhiteSpace(correctAnswer))
                    {
                        return correctAnswer;
                    }

                    if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
                    {
                        var json = choices.GetRawText();
                        return TryGetCorrectAnswerFromChoices(json);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetBool(JsonElement obj, string propName)
        {
            if (!obj.TryGetProperty(propName, out var prop))
            {
                return false;
            }

            if (prop.ValueKind == JsonValueKind.True) return true;
            if (prop.ValueKind == JsonValueKind.False) return false;

            if (prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out bool b))
            {
                return b;
            }

            return false;
        }

        private static string? TryGetString(JsonElement obj, string propName)
        {
            if (!obj.TryGetProperty(propName, out var prop))
            {
                return null;
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }

            return null;
        }

        private void UpdateUserProgressAfterScene(int userId)
        {
            var now = _dateTimeProvider.UtcNow;

            var progress = _dbContext.UserProgresses
                .FirstOrDefault(x => x.UserId == userId);

            int lessonsScore = _dbContext
                .LessonResults
                .Where(x => x.UserId == userId)
                .GroupBy(x => x.LessonId)
                .Select(g => g.Max(x => x.Score))
                .Sum();

            int completedScenesCount = _dbContext.SceneAttempts
                .Count(x => x.UserId == userId && x.IsCompleted);

            int scenesScore = completedScenesCount * _learningSettings.SceneCompletionScore;

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
            }
            else
            {
                progress.TotalScore = lessonsScore + scenesScore;
                progress.LastUpdatedAt = now;
            }

            _dbContext.SaveChanges();
        }


        private bool GetIsSceneUnlockedByTopic(int userId, int topicId)
        {
            var lessonIds = _dbContext.Lessons
                .Where(x => x.TopicId == topicId)
                .Select(x => x.Id)
                .ToList();

            if (lessonIds.Count == 0)
            {
                return true;
            }

            var passedLessonIds = _dbContext.LessonResults
                .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonId) && x.TotalQuestions > 0)
                .AsEnumerable()
                .Where(x => x.Score * 100 >= x.TotalQuestions * _learningSettings.PassingScorePercent)
                .Select(x => x.LessonId)
                .Distinct()
                .ToHashSet();

            return passedLessonIds.Count == lessonIds.Count;
        }

        private string? GetSceneUnlockReason(Scene scene, bool isUnlocked, int required, int passedLessons)
        {
            if (isUnlocked)
            {
                return null;
            }

            if (scene.TopicId.HasValue)
            {
                return "Pass all lessons in the topic to unlock";
            }

            return $"Pass {required} lessons to unlock";
        }

    }
}
