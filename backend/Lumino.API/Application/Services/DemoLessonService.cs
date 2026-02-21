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

        public List<LessonResponse> GetDemoLessons()
        {
            var ids = NormalizeLessonIds(_demoSettings.LessonIds);

            var result = new List<LessonResponse>();

            foreach (var id in ids)
            {
                result.Add(GetDemoLessonById(id));
            }

            return result;
        }

        public DemoNextLessonResponse GetDemoNextLesson(int step)
        {
            var ids = NormalizeLessonIds(_demoSettings.LessonIds);

            if (ids.Count == 0)
            {
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
                Lesson = GetDemoLessonById(lessonId)
            };
        }

        public DemoNextLessonPackResponse GetDemoNextLessonPack(int step)
        {
            var ids = NormalizeLessonIds(_demoSettings.LessonIds);

            if (ids.Count == 0)
            {
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
                Lesson = GetDemoLessonById(lessonId),
                Exercises = GetDemoExercisesByLesson(lessonId)
            };
        }

        public LessonResponse GetDemoLessonById(int lessonId)
        {
            EnsureDemoLessonAllowed(lessonId);

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

        public List<ExerciseResponse> GetDemoExercisesByLesson(int lessonId)
        {
            EnsureDemoLessonAllowed(lessonId);

            // Validate lesson/topic/course published
            GetDemoLessonById(lessonId);

            return _dbContext.Exercises
                .Where(x => x.LessonId == lessonId)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
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

        public SubmitLessonResponse SubmitDemoLesson(SubmitLessonRequest request)
        {
            _submitLessonRequestValidator.Validate(request);

            EnsureDemoLessonAllowed(request.LessonId);

            // Validate lesson/topic/course published
            GetDemoLessonById(request.LessonId);

            var exercises = _dbContext.Exercises
                .Where(x => x.LessonId == request.LessonId)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
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

        private void EnsureDemoLessonAllowed(int lessonId)
        {
            var ids = NormalizeLessonIds(_demoSettings.LessonIds);

            if (!ids.Contains(lessonId))
            {
                throw new ForbiddenAccessException("Demo lesson is not available");
            }
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

            var correctAnswer = (exercise.CorrectAnswer ?? string.Empty);

            return !string.IsNullOrWhiteSpace(userAnswerText) &&
                Normalize(userAnswerText) == Normalize(correctAnswer);
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
