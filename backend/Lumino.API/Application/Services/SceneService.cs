using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
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
                .OrderBy(x => x.Id)
                .Select(x => new SceneResponse
                {
                    Id = x.Id,
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

            var passedLessons = GetPassedDistinctLessonsCount(userId);

            var required = SceneUnlockRules.GetRequiredPassedLessons(sceneId, _learningSettings.SceneUnlockEveryLessons);
            var isUnlocked = SceneUnlockRules.IsUnlocked(sceneId, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            var isCompleted = _dbContext.SceneAttempts
                .Any(x => x.UserId == userId && x.SceneId == sceneId && x.IsCompleted);

            return new SceneDetailsResponse
            {
                Id = scene.Id,
                Title = scene.Title,
                Description = scene.Description,
                SceneType = scene.SceneType,
                BackgroundUrl = scene.BackgroundUrl,
                AudioUrl = scene.AudioUrl,
                IsCompleted = isCompleted,
                IsUnlocked = isUnlocked,
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

            var passedLessons = GetPassedDistinctLessonsCount(userId);

            var required = SceneUnlockRules.GetRequiredPassedLessons(sceneId, _learningSettings.SceneUnlockEveryLessons);
            var isUnlocked = SceneUnlockRules.IsUnlocked(sceneId, passedLessons, _learningSettings.SceneUnlockEveryLessons);

            var isCompleted = _dbContext.SceneAttempts
                .Any(x => x.UserId == userId && x.SceneId == sceneId && x.IsCompleted);

            var steps = new List<SceneStepResponse>();

            if (isUnlocked)
            {
                steps = _dbContext.SceneSteps
                    .Where(x => x.SceneId == sceneId)
                    .OrderBy(x => x.Order)
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
                Title = scene.Title,
                Description = scene.Description,
                SceneType = scene.SceneType,
                BackgroundUrl = scene.BackgroundUrl,
                AudioUrl = scene.AudioUrl,
                IsCompleted = isCompleted,
                IsUnlocked = isUnlocked,
                PassedLessons = passedLessons,
                RequiredPassedLessons = required,
                Steps = steps
            };
        }

        public SubmitSceneResponse SubmitScene(int userId, int sceneId, SubmitSceneRequest request)
        {
            _submitSceneRequestValidator.Validate(request);

            var scene = _dbContext.Scenes.FirstOrDefault(x => x.Id == sceneId);

            if (scene == null)
            {
                throw new KeyNotFoundException("Scene not found");
            }

            var passedLessons = GetPassedDistinctLessonsCount(userId);

            if (!SceneUnlockRules.IsUnlocked(sceneId, passedLessons, _learningSettings.SceneUnlockEveryLessons))
            {
                throw new ForbiddenAccessException("Scene is locked");
            }

            var steps = _dbContext.SceneSteps
                .Where(x => x.SceneId == sceneId)
                .OrderBy(x => x.Order)
                .ToList();

            var questionSteps = steps
                .Where(x => !string.IsNullOrWhiteSpace(x.ChoicesJson))
                .ToList();

            int totalQuestions = questionSteps.Count;

            // якщо в сцені немає choices (тільки діалоги/контент) — submit завершує сцену як “пройдено”
            if (totalQuestions == 0)
            {
                EnsureCompletedAttempt(userId, sceneId, score: 0, totalQuestions: 0, detailsJson: null);
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

            bool isCompleted = correct == totalQuestions;

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
                markCompleted: isCompleted
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

            // заборона “закрити” сцену, якщо вона ще locked
            var passedLessons = GetPassedDistinctLessonsCount(userId);

            if (!SceneUnlockRules.IsUnlocked(sceneId, passedLessons, _learningSettings.SceneUnlockEveryLessons))
            {
                throw new ForbiddenAccessException("Scene is locked");
            }

            var exists = _dbContext.SceneAttempts
                .Any(x => x.UserId == userId && x.SceneId == sceneId);

            if (exists) return;

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

        private void EnsureCompletedAttempt(int userId, int sceneId, int score, int totalQuestions, string? detailsJson, bool markCompleted = true)
        {
            var now = _dateTimeProvider.UtcNow;

            var attempt = _dbContext.SceneAttempts
                .FirstOrDefault(x => x.UserId == userId && x.SceneId == sceneId);

            // якщо вже пройдено — idempotent (не перераховуємо прогрес/ачівки)
            if (attempt != null && attempt.IsCompleted)
            {
                return;
            }

            bool wasCompleted = attempt != null && attempt.IsCompleted;

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
            }

            attempt.Score = score;
            attempt.TotalQuestions = totalQuestions;
            attempt.DetailsJson = detailsJson;

            if (markCompleted)
            {
                attempt.IsCompleted = true;
                attempt.CompletedAt = now;
            }

            _dbContext.SaveChanges();

            if (markCompleted && !wasCompleted)
            {
                UpdateUserProgressAfterScene(userId);
                _achievementService.CheckAndGrantSceneAchievements(userId);
            }
        }

        private static bool IsAnswerCorrect(string userAnswer, string correctAnswer)
        {
            return NormalizeAnswer(userAnswer) == NormalizeAnswer(correctAnswer);
        }

        private static string NormalizeAnswer(string value)
        {
            return (value ?? "")
                .Trim()
                .ToLowerInvariant();
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
