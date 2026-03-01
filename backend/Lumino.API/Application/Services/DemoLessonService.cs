using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Collections.Generic;

namespace Lumino.Api.Application.Services
{
    public class DemoLessonService : IDemoLessonService
    {
        private const int DemoLessonsCount = 1;
        private const int DemoExercisesCount = 3;

        private readonly LuminoDbContext _dbContext;
        private readonly ISubmitLessonRequestValidator _submitLessonRequestValidator;
        private readonly LearningSettings _learningSettings;
        private readonly DemoSettings _demoSettings;

        public DemoLessonService(
            LuminoDbContext dbContext,
            ISubmitLessonRequestValidator submitLessonRequestValidator,
            IOptions<LearningSettings> learningSettings,
            IOptions<DemoSettings> demoSettings)
        {
            _dbContext = dbContext;
            _submitLessonRequestValidator = submitLessonRequestValidator;
            _learningSettings = learningSettings.Value;
            _demoSettings = demoSettings.Value;
        }

        public List<LessonResponse> GetDemoLessons(string? languageCode = null, string? level = null)
        {
            var ids = GetLessonIds(languageCode, level);

            var result = new List<LessonResponse>();

            foreach (var id in ids)
            {
                result.Add(GetDemoLessonById(id, languageCode, level));
            }

            return result;
        }

        public DemoNextLessonResponse GetDemoNextLesson(int step, string? languageCode = null, string? level = null)
        {
            var resolved = ResolveLessonIds(languageCode, level);

            var ids = resolved.Ids;
            if (ids.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(languageCode))
                {
                    throw new KeyNotFoundException("Courses for selected language are in development. Please choose another language.");
                }

                throw new KeyNotFoundException("Demo lessons are not configured");
            }

            if (step < 0 || step >= ids.Count)
            {
                throw new KeyNotFoundException("Demo step not found");
            }

            var lessonId = ids[step];

            var isLast = step == ids.Count - 1;

            var lessonNumberText = $"Урок {step + 1} з {ids.Count}";

            return new DemoNextLessonResponse
            {
                Step = step,
                StepNumber = step + 1,
                Total = ids.Count,
                IsLast = isLast,
                CtaText = isLast ? "Щоб зберегти прогрес — зареєструйся" : string.Empty,
                ShowRegisterCta = isLast,
                LessonNumberText = lessonNumberText,
                IsFallbackToA1 = resolved.IsFallbackToA1,
                ResolvedLevel = resolved.ResolvedLevel,
                FallbackMessage = resolved.FallbackMessage,
                Lesson = GetDemoLessonById(lessonId, languageCode, level)
            };
        }

        public DemoNextLessonPackResponse GetDemoNextLessonPack(int step, string? languageCode = null, string? level = null)
        {
            var resolved = ResolveLessonIds(languageCode, level);

            var ids = resolved.Ids;
            if (ids.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(languageCode))
                {
                    throw new KeyNotFoundException("Courses for selected language are in development. Please choose another language.");
                }

                throw new KeyNotFoundException("Demo lessons are not configured");
            }

            if (step < 0 || step >= ids.Count)
            {
                throw new KeyNotFoundException("Demo step not found");
            }

            var lessonId = ids[step];

            var isLast = step == ids.Count - 1;
            var lessonNumberText = $"Урок {step + 1} з {ids.Count}";

            return new DemoNextLessonPackResponse
            {
                Step = step,
                StepNumber = step + 1,
                Total = ids.Count,
                IsLast = isLast,
                CtaText = isLast ? "Щоб зберегти прогрес — зареєструйся" : string.Empty,
                ShowRegisterCta = isLast,
                LessonNumberText = lessonNumberText,
                IsFallbackToA1 = resolved.IsFallbackToA1,
                ResolvedLevel = resolved.ResolvedLevel,
                FallbackMessage = resolved.FallbackMessage,
                Lesson = GetDemoLessonById(lessonId, languageCode, level),
                Exercises = GetDemoExercisesByLesson(lessonId, languageCode, level)
            };
        }

        public LessonResponse GetDemoLessonById(int lessonId, string? languageCode = null, string? level = null)
        {
            EnsureDemoLessonAllowed(lessonId, languageCode, level);

            var lesson = _dbContext.Lessons.FirstOrDefault(x => x.Id == lessonId);

            if (lesson == null)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            var topic = _dbContext.Topics.FirstOrDefault(x => x.Id == lesson.TopicId);

            if (topic == null)
            {
                throw new KeyNotFoundException("Topic not found");
            }

            var course = _dbContext.Courses.FirstOrDefault(x => x.Id == topic.CourseId && x.IsPublished);

            if (course == null)
            {
                throw new KeyNotFoundException("Course not found");
            }

            return new LessonResponse
            {
                Id = lesson.Id,
                TopicId = lesson.TopicId,
                Title = lesson.Title,
                Theory = lesson.Theory,
                Order = lesson.Order
            };
        }

        public List<ExerciseResponse> GetDemoExercisesByLesson(int lessonId, string? languageCode = null, string? level = null)
        {
            EnsureDemoLessonAllowed(lessonId, languageCode, level);

            // Validate lesson/topic/course published
            GetDemoLessonById(lessonId, languageCode, level);

            return _dbContext.Exercises
                .Where(x => x.LessonId == lessonId)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Take(DemoExercisesCount)
                .Select(x => new ExerciseResponse
                {
                    Id = x.Id,
                    Type = x.Type.ToString(),
                    Question = x.Question,
                    Data = x.Data,
                    Order = x.Order
                })
                .ToList();
        }

        public SubmitLessonResponse SubmitDemoLesson(SubmitLessonRequest request, string? languageCode = null, string? level = null)
        {
            _submitLessonRequestValidator.Validate(request);

            EnsureDemoLessonAllowed(request.LessonId, languageCode, level);

            // Validate lesson/topic/course published
            GetDemoLessonById(request.LessonId, languageCode, level);

            var exercises = _dbContext.Exercises
                .Where(x => x.LessonId == request.LessonId)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .Take(DemoExercisesCount)
                .ToList();

            int correct = 0;
            var mistakeExerciseIds = new List<int>();
            var answers = new List<LessonAnswerResultDto>();

            foreach (var exercise in exercises)
            {
                var userAnswer = request.Answers
                    .FirstOrDefault(x => x.ExerciseId == exercise.Id);

                var userAnswerText = userAnswer != null
                    ? (userAnswer.Answer ?? string.Empty)
                    : string.Empty;

                var isCorrect = IsExerciseCorrect(exercise, userAnswerText);

                var correctAnswerForResponse = exercise.Type == ExerciseType.Match
                    ? (exercise.Data ?? string.Empty)
                    : (exercise.CorrectAnswer ?? string.Empty);

                answers.Add(new LessonAnswerResultDto
                {
                    ExerciseId = exercise.Id,
                    UserAnswer = userAnswerText,
                    CorrectAnswer = correctAnswerForResponse,
                    IsCorrect = isCorrect
                });

                if (isCorrect)
                {
                    correct++;
                    continue;
                }

                mistakeExerciseIds.Add(exercise.Id);
            }

            var passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);
            var isPassed = LessonPassingRules.IsPassed(correct, exercises.Count, passingScorePercent);

            return new SubmitLessonResponse
            {
                TotalExercises = exercises.Count,
                CorrectAnswers = correct,
                IsPassed = isPassed,
                MistakeExerciseIds = mistakeExerciseIds,
                Answers = answers
            };
        }

        private void EnsureDemoLessonAllowed(int lessonId, string? languageCode, string? level)
        {
            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                var idsByLang = GetLessonIds(languageCode, level);

                if (!idsByLang.Contains(lessonId))
                {
                    throw new ForbiddenAccessException("Demo lesson is not available");
                }

                return;
            }

            // When languageCode is not provided, allow any demo lesson from any configured language.
            var allIds = new List<int>();

            allIds.AddRange(NormalizeLessonIds(_demoSettings.LessonIds)
                .Take(DemoLessonsCount));

            if (_demoSettings.LanguageLessonIds != null && _demoSettings.LanguageLessonIds.Count > 0)
            {
                foreach (var kv in _demoSettings.LanguageLessonIds)
                {
                    if (kv.Value == null)
                    {
                        continue;
                    }

                    allIds.AddRange(NormalizeLessonIds(kv.Value)
                        .Take(DemoLessonsCount));
                }
            }

            if (!allIds.Distinct().Contains(lessonId))
            {
                throw new ForbiddenAccessException("Demo lesson is not available");
            }
        }


        private string? NormalizeLevelKey(string? level)
        {
            if (string.IsNullOrWhiteSpace(level))
            {
                return null;
            }

            var v = level.Trim().ToLowerInvariant();

            // CEFR levels
            if (v == "a1" || v == "a2" || v == "b1" || v == "b2" || v == "c1" || v == "c2")
            {
                return v;
            }

            // Ukrainian onboarding labels
            if (v == "новачок")
            {
                return "a1";
            }

            if (v == "початківець")
            {
                return "a2";
            }

            if (v == "впевнено")
            {
                return "b1";
            }

            if (v == "просунуто")
            {
                return "b2";
            }

            if (v == "вільно")
            {
                return "c1";
            }

            // English labels (optional)
            if (v == "newbie" || v == "starter" || v == "beginner")
            {
                return "a1";
            }

            if (v == "elementary")
            {
                return "a2";
            }

            if (v == "intermediate" || v == "confident")
            {
                return "b1";
            }

            if (v == "advanced")
            {
                return "b2";
            }

            if (v == "fluent")
            {
                return "c1";
            }

            throw new ArgumentException("Level is not supported");
        }


        private class DemoResolution
        {
            public List<int> Ids { get; set; } = new List<int>();
            public string ResolvedLevel { get; set; } = string.Empty;
            public bool IsFallbackToA1 { get; set; }
            public string FallbackMessage { get; set; } = string.Empty;
        }

        private DemoResolution ResolveLessonIds(string? languageCode, string? level)
        {
            var resolution = new DemoResolution();

            if (string.IsNullOrWhiteSpace(languageCode))
            {
                resolution.Ids = GetLessonIds(languageCode, level);
                return resolution;
            }

            if (!SupportedLanguages.IsLearnable(languageCode))
            {
                throw new ArgumentException("LanguageCode is not supported");
            }

            var normalized = languageCode.Trim().ToLowerInvariant();
            var normalizedLevel = NormalizeLevelKey(level);

            // when config mapping is used (only for no level), we can't reliably resolve a level
            if (normalizedLevel == null &&
                _demoSettings.LanguageLessonIds != null &&
                _demoSettings.LanguageLessonIds.TryGetValue(normalized, out var ids) &&
                ids != null &&
                ids.Count > 0)
            {
                resolution.Ids = NormalizeLessonIds(ids)
                    .Take(DemoLessonsCount)
                    .ToList();
                return resolution;
            }

            var coursesQuery = _dbContext.Courses
                .Where(x => x.IsPublished && x.LanguageCode == normalized);

            var courses = coursesQuery
                .Select(x => new { x.Id, Title = (x.Title ?? string.Empty).ToLower() })
                .OrderBy(x => x.Id)
                .ToList();

            if (courses == null || courses.Count == 0)
            {
                resolution.Ids = new List<int>();
                return resolution;
            }

            int courseId = 0;

            if (normalizedLevel != null)
            {
                var exact = courses.FirstOrDefault(x => x.Title.Contains(normalizedLevel));

                if (exact != null)
                {
                    courseId = exact.Id;
                    resolution.ResolvedLevel = normalizedLevel;
                }
                else
                {
                    var a1 = courses.FirstOrDefault(x => x.Title.Contains("a1"));

                    if (a1 != null)
                    {
                        courseId = a1.Id;
                        resolution.ResolvedLevel = "a1";
                        resolution.IsFallbackToA1 = true;
                        resolution.FallbackMessage = $"Рівень {normalizedLevel.ToUpper()} ще в розробці. Показуємо демо рівня A1.";
                    }
                    else
                    {
                        courseId = courses.First().Id;
                        resolution.ResolvedLevel = string.Empty;
                        resolution.IsFallbackToA1 = true;
                        resolution.FallbackMessage = $"Рівень {normalizedLevel.ToUpper()} ще в розробці. Показуємо демо з доступного курсу.";
                    }
                }
            }
            else
            {
                var a1 = courses.FirstOrDefault(x => x.Title.Contains("a1"));

                if (a1 != null)
                {
                    courseId = a1.Id;
                    resolution.ResolvedLevel = "a1";
                }
                else
                {
                    courseId = courses.First().Id;
                    resolution.ResolvedLevel = string.Empty;
                }
            }

            if (courseId <= 0)
            {
                resolution.Ids = new List<int>();
                return resolution;
            }

            var lessonIds = _dbContext.Lessons
                .Join(
                    _dbContext.Topics.Where(t => t.CourseId == courseId),
                    l => l.TopicId,
                    t => t.Id,
                    (l, t) => new { Lesson = l, Topic = t }
                )
                .OrderBy(x => x.Topic.Order <= 0 ? int.MaxValue : x.Topic.Order)
                .ThenBy(x => x.Topic.Id)
                .ThenBy(x => x.Lesson.Order <= 0 ? int.MaxValue : x.Lesson.Order)
                .ThenBy(x => x.Lesson.Id)
                .Select(x => x.Lesson.Id)
                .Take(DemoLessonsCount)
                .ToList();

            resolution.Ids = lessonIds != null && lessonIds.Count > 0
                ? NormalizeLessonIds(lessonIds)
                : new List<int>();

            // IMPORTANT: When languageCode is specified and no demo can be resolved,
            // we must NOT fallback to other language demo lessons.
            return resolution;
        }

        private List<int> GetLessonIds(string? languageCode, string? level)
        {
            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                if (!SupportedLanguages.IsLearnable(languageCode))
                {
                    throw new ArgumentException("LanguageCode is not supported");
                }

                var normalized = languageCode.Trim().ToLowerInvariant();
                var normalizedLevel = NormalizeLevelKey(level);

                if (normalizedLevel == null &&
                    _demoSettings.LanguageLessonIds != null &&
                    _demoSettings.LanguageLessonIds.TryGetValue(normalized, out var ids) &&
                    ids != null &&
                    ids.Count > 0)
                {
                    return NormalizeLessonIds(ids)
                        .Take(DemoLessonsCount)
                        .ToList();
                }

                // If config mapping is not provided - try to auto-pick first 3 lessons
                // from the published course of the selected language.
                // Prefer selected level course by title (a1/a2/b1/b2/c1/c2),
                // fallback to A1 if exists, otherwise use the first published course.
                var coursesQuery = _dbContext.Courses
                    .Where(x => x.IsPublished && x.LanguageCode == normalized);

                var courseId = normalizedLevel != null
                    ? coursesQuery
                        .OrderByDescending(x => (x.Title ?? string.Empty).ToLower().Contains(normalizedLevel))
                        .ThenByDescending(x => (x.Title ?? string.Empty).ToLower().Contains("a1"))
                        .ThenBy(x => x.Id)
                        .Select(x => x.Id)
                        .FirstOrDefault()
                    : coursesQuery
                        .OrderByDescending(x => (x.Title ?? string.Empty).ToLower().Contains("a1"))
                        .ThenBy(x => x.Id)
                        .Select(x => x.Id)
                        .FirstOrDefault();

                if (courseId > 0)
                {
                    var lessonIds = _dbContext.Lessons
                        .Join(
                            _dbContext.Topics.Where(t => t.CourseId == courseId),
                            l => l.TopicId,
                            t => t.Id,
                            (l, t) => new { Lesson = l, Topic = t }
                        )
                        .OrderBy(x => x.Topic.Order <= 0 ? int.MaxValue : x.Topic.Order)
                        .ThenBy(x => x.Topic.Id)
                        .ThenBy(x => x.Lesson.Order <= 0 ? int.MaxValue : x.Lesson.Order)
                        .ThenBy(x => x.Lesson.Id)
                        .Select(x => x.Lesson.Id)
                        .Take(DemoLessonsCount)
                        .ToList();

                    if (lessonIds != null && lessonIds.Count > 0)
                    {
                        return NormalizeLessonIds(lessonIds);
                    }
                }
                // IMPORTANT: When languageCode is specified and no demo can be resolved,
                // we must NOT fallback to other language demo lessons.
                return new List<int>();
            }

            return NormalizeLessonIds(_demoSettings.LessonIds)
                .Take(DemoLessonsCount)
                .ToList();
        }
        private List<int> NormalizeLessonIds(List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return new List<int>();
            }

            return ids
                .Where(x => x > 0)
                .Distinct()
                .ToList();
        }

        private bool IsExerciseCorrect(Exercise exercise, string userAnswerText)
        {
            if (exercise == null)
            {
                return false;
            }

            userAnswerText = userAnswerText ?? string.Empty;

            if (exercise.Type == ExerciseType.Match)
            {
                return IsMatchCorrect(exercise.Data, userAnswerText);
            }

            var correctAnswerRaw = (exercise.CorrectAnswer ?? string.Empty);

            if (exercise.Type == ExerciseType.Input)
            {
                var answers = ParseCorrectAnswers(correctAnswerRaw);

                if (answers.Count == 0)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(userAnswerText))
                {
                    return false;
                }

                var normalizedUser = Normalize(userAnswerText);

                return answers.Any(x => Normalize(x) == normalizedUser);
            }

            return !string.IsNullOrWhiteSpace(userAnswerText) &&
                Normalize(userAnswerText) == Normalize(correctAnswerRaw);
        }

        private static List<string> ParseCorrectAnswers(string correctAnswer)
        {
            if (correctAnswer == null)
            {
                return new List<string>();
            }

            var trimmed = correctAnswer.Trim();

            if (trimmed.Length == 0)
            {
                return new List<string>();
            }

            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(trimmed);
                    return list ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }

            return new List<string> { correctAnswer };
        }

        private bool IsMatchCorrect(string dataJson, string userJson)
        {
            if (string.IsNullOrWhiteSpace(dataJson) || string.IsNullOrWhiteSpace(userJson))
            {
                return false;
            }

            List<MatchPair>? correctPairs;
            List<MatchPair>? userPairs;

            try
            {
                correctPairs = JsonSerializer.Deserialize<List<MatchPair>>(dataJson);
                userPairs = JsonSerializer.Deserialize<List<MatchPair>>(userJson);
            }
            catch
            {
                return false;
            }

            if (correctPairs == null || userPairs == null)
            {
                return false;
            }

            if (correctPairs.Count == 0 || userPairs.Count == 0)
            {
                return false;
            }

            var correctMap = new Dictionary<string, string>();

            foreach (var p in correctPairs)
            {
                var left = Normalize(p.left);
                var right = Normalize(p.right);

                if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                {
                    continue;
                }

                if (!correctMap.ContainsKey(left))
                {
                    correctMap[left] = right;
                }
            }

            var userMap = new Dictionary<string, string>();

            foreach (var p in userPairs)
            {
                var left = Normalize(p.left);
                var right = Normalize(p.right);

                if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                {
                    continue;
                }

                if (!userMap.ContainsKey(left))
                {
                    userMap[left] = right;
                }
            }

            if (correctMap.Count == 0 || userMap.Count == 0)
            {
                return false;
            }

            if (userMap.Count != correctMap.Count)
            {
                return false;
            }

            foreach (var kv in correctMap)
            {
                if (!userMap.TryGetValue(kv.Key, out var userRight))
                {
                    return false;
                }

                if (userRight != kv.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private class MatchPair
        {
            public string left { get; set; } = null!;
            public string right { get; set; } = null!;
        }

        private static string Normalize(string value)
        {
            return AnswerNormalizer.Normalize(value);
        }
    }
}
