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
        private readonly ISubmitSceneRequestValidator _submitSceneRequestValidator;
        private readonly LearningSettings _learningSettings;

        public SceneService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IAchievementService achievementService,
            IOptions<LearningSettings> learningSettings)
            : this(
                dbContext,
                dateTimeProvider,
                achievementService,
                learningSettings,
                new SubmitSceneRequestValidator())
        {
        }

        public SceneService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IAchievementService achievementService,
            IOptions<LearningSettings> learningSettings,
            ISubmitSceneRequestValidator submitSceneRequestValidator)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _achievementService = achievementService;
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
                    Order = x.Order,
                    Title = x.Title,
                    Description = x.Description,
                    SceneType = x.SceneType,
                    BackgroundUrl = x.BackgroundUrl,
                    AudioUrl = x.AudioUrl
                })
                .ToList();
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
            var isUnlocked = SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            var unlockReason = isUnlocked ? null : $"Pass {required} lessons to unlock";

            var isCompleted = _dbContext.SceneAttempts
                .Any(x => x.UserId == userId && x.SceneId == sceneId && x.IsCompleted);

            return new SceneDetailsResponse
            {
                Id = scene.Id,
                CourseId = scene.CourseId,
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
            var isUnlocked = SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            var unlockReason = isUnlocked ? null : $"Pass {required} lessons to unlock";

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

            if (!SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons))
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

            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == sceneId);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var passedLessons = GetPassedDistinctLessonsCount(userId, scene.CourseId);
            var scenePosition = GetScenePosition(scene);

            if (!SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons))
            {
                throw new ForbiddenAccessException("Scene is locked");
            }


            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existingAttempt = _dbContext.SceneAttempts
                    .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

                if (existingAttempt != null
                    && !string.IsNullOrWhiteSpace(existingAttempt.IdempotencyKey)
                    && string.Equals(existingAttempt.IdempotencyKey, request.IdempotencyKey, StringComparison.Ordinal))
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
                EnsureCompletedAttempt(userId, sceneId, score: 0, totalQuestions: 0, detailsJson: null, idempotencyKey: request.IdempotencyKey);
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
                    idempotencyKey: request.IdempotencyKey
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

            EnsureCompletedAttempt(
                userId,
                sceneId,
                score: correct,
                totalQuestions: totalQuestions,
                detailsJson: detailsJson,
                markCompleted: completed,
                idempotencyKey: request.IdempotencyKey
            );

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

            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == sceneId);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var passedLessons = GetPassedDistinctLessonsCount(userId, scene.CourseId);
            var scenePosition = GetScenePosition(scene);

            if (!SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons))
            {
                throw new ForbiddenAccessException("Scene is locked");
            }


            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existingAttempt = _dbContext.SceneAttempts
                    .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

                if (existingAttempt != null
                    && !string.IsNullOrWhiteSpace(existingAttempt.IdempotencyKey)
                    && string.Equals(existingAttempt.IdempotencyKey, request.IdempotencyKey, StringComparison.Ordinal))
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

            // якщо в сцені немає choices (тільки діалоги/контент) — submit завершує сцену як “пройдено”
            if (totalQuestions == 0)
            {
                EnsureCompletedAttempt(userId, sceneId, score: 0, totalQuestions: 0, detailsJson: null, idempotencyKey: request.IdempotencyKey);
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
                idempotencyKey: request.IdempotencyKey
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

            if (!SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, _learningSettings.SceneUnlockEveryLessons))
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

            _dbContext.SaveChanges();

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

        private void EnsureCompletedAttempt(int userId, int sceneId, int score, int totalQuestions, string? detailsJson, bool markCompleted = true, string? idempotencyKey = null)
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

                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    attempt.IdempotencyKey = idempotencyKey;
                }

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

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                attempt.IdempotencyKey = idempotencyKey;
            }

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

                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    attempt.IdempotencyKey = idempotencyKey;
                }

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
                _achievementService.CheckAndGrantSceneAchievements(userId);
            }
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
    }
}
