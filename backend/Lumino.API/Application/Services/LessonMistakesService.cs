using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Lumino.Api.Application.Services
{
    public class LessonMistakesService : ILessonMistakesService
    {
        private readonly LuminoDbContext _dbContext;

        public LessonMistakesService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public LessonMistakesResponse GetLessonMistakes(int userId, int lessonId)
        {
            var lesson = _dbContext.Lessons.FirstOrDefault(x => x.Id == lessonId);

            if (lesson == null)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            var progress = _dbContext.UserLessonProgresses
                .FirstOrDefault(x => x.UserId == userId && x.LessonId == lesson.Id);

            if (progress == null || !progress.IsUnlocked)
            {
                throw new ForbiddenAccessException("Lesson is locked");
            }

            var lastResult = _dbContext.LessonResults
                .Where(x => x.UserId == userId && x.LessonId == lesson.Id)
                .OrderByDescending(x => x.CompletedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            if (lastResult == null || string.IsNullOrWhiteSpace(lastResult.MistakesJson))
            {
                return new LessonMistakesResponse
                {
                    LessonId = lessonId,
                    TotalMistakes = 0,
                    MistakeExerciseIds = new List<int>(),
                    Exercises = new List<ExerciseResponse>()
                };
            }

            var details = ParseDetails(lastResult.MistakesJson);

            if (details == null || details.MistakeExerciseIds == null || details.MistakeExerciseIds.Count == 0)
            {
                return new LessonMistakesResponse
                {
                    LessonId = lessonId,
                    TotalMistakes = 0,
                    MistakeExerciseIds = new List<int>(),
                    Exercises = new List<ExerciseResponse>()
                };
            }

            var allExercises = _dbContext.Exercises
                .Where(x => x.LessonId == lesson.Id)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            var allIds = allExercises.Select(x => x.Id).ToHashSet();

            var mistakeIds = details.MistakeExerciseIds
                .Where(x => allIds.Contains(x))
                .Distinct()
                .ToList();

            if (mistakeIds.Count == 0)
            {
                return new LessonMistakesResponse
                {
                    LessonId = lessonId,
                    TotalMistakes = 0,
                    MistakeExerciseIds = new List<int>(),
                    Exercises = new List<ExerciseResponse>()
                };
            }

            var exercises = allExercises
                .Where(x => mistakeIds.Contains(x.Id))
                .ToList();

            return new LessonMistakesResponse
            {
                LessonId = lessonId,
                TotalMistakes = mistakeIds.Count,
                MistakeExerciseIds = mistakeIds,
                Exercises = exercises.Select(MapExercise).ToList()
            };
        }

        public SubmitLessonMistakesResponse SubmitLessonMistakes(int userId, int lessonId, SubmitLessonMistakesRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var lesson = _dbContext.Lessons.FirstOrDefault(x => x.Id == lessonId);

            if (lesson == null)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            var progress = _dbContext.UserLessonProgresses
                .FirstOrDefault(x => x.UserId == userId && x.LessonId == lesson.Id);

            if (progress == null || !progress.IsUnlocked)
            {
                throw new ForbiddenAccessException("Lesson is locked");
            }

            var allExercises = _dbContext.Exercises
                .Where(x => x.LessonId == lesson.Id)
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            var totalExercises = allExercises.Count;

            var lastResult = _dbContext.LessonResults
                .Where(x => x.UserId == userId && x.LessonId == lesson.Id)
                .OrderByDescending(x => x.CompletedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            if (lastResult == null || string.IsNullOrWhiteSpace(lastResult.MistakesJson))
            {
                return new SubmitLessonMistakesResponse
                {
                    LessonId = lessonId,
                    TotalExercises = totalExercises,
                    CorrectAnswers = 0,
                    IsCompleted = true,
                    MistakeExerciseIds = new List<int>(),
                    Answers = new List<LessonAnswerResultDto>()
                };
            }

            var details = ParseDetails(lastResult.MistakesJson);

            if (details == null)
            {
                details = new LessonResultDetailsJson();
            }

            if (details.Answers == null)
            {
                details.Answers = new List<LessonAnswerResultDto>();
            }

            if (details.MistakeExerciseIds == null)
            {
                details.MistakeExerciseIds = new List<int>();
            }

            var allExerciseIds = allExercises.Select(x => x.Id).ToHashSet();

            var targetMistakeIds = details.MistakeExerciseIds
                .Where(x => allExerciseIds.Contains(x))
                .Distinct()
                .ToList();

            if (targetMistakeIds.Count == 0)
            {
                EnsureDetailsContainsAllExercises(details, allExercises);

                details.MistakeExerciseIds = details.Answers
                    .Where(x => !x.IsCorrect)
                    .Select(x => x.ExerciseId)
                    .Distinct()
                    .ToList();

                var correctCount = details.Answers.Count(x => x.IsCorrect);
                var completed = details.MistakeExerciseIds.Count == 0;

                lastResult.Score = correctCount;
                lastResult.TotalQuestions = totalExercises;
                lastResult.MistakesJson = SerializeDetails(details);

                _dbContext.SaveChanges();

                return new SubmitLessonMistakesResponse
                {
                    LessonId = lessonId,
                    TotalExercises = totalExercises,
                    CorrectAnswers = correctCount,
                    IsCompleted = completed,
                    MistakeExerciseIds = details.MistakeExerciseIds,
                    Answers = details.Answers
                };
            }

            var answersMap = new Dictionary<int, string>();

            foreach (var a in request.Answers ?? new List<SubmitExerciseAnswerRequest>())
            {
                if (!answersMap.ContainsKey(a.ExerciseId))
                {
                    answersMap.Add(a.ExerciseId, a.Answer ?? string.Empty);
                    continue;
                }

                throw new ArgumentException("Duplicate ExerciseId in answers");
            }

            var existing = details.Answers.ToDictionary(x => x.ExerciseId, x => x);

            foreach (var exerciseId in targetMistakeIds)
            {
                var exercise = allExercises.First(x => x.Id == exerciseId);

                answersMap.TryGetValue(exercise.Id, out string? newUserAnswer);

                if (string.IsNullOrWhiteSpace(newUserAnswer) && existing.TryGetValue(exercise.Id, out var prev))
                {
                    newUserAnswer = prev.UserAnswer;
                }

                newUserAnswer ??= string.Empty;

                var isCorrect = IsExerciseCorrect(exercise, newUserAnswer);

                var correctAnswerForResponse = exercise.Type == ExerciseType.Match
                    ? (exercise.Data ?? string.Empty)
                    : (exercise.CorrectAnswer ?? string.Empty);

                if (existing.TryGetValue(exercise.Id, out var dto))
                {
                    dto.UserAnswer = newUserAnswer;
                    dto.CorrectAnswer = correctAnswerForResponse;
                    dto.IsCorrect = isCorrect;
                }
                else
                {
                    existing[exercise.Id] = new LessonAnswerResultDto
                    {
                        ExerciseId = exercise.Id,
                        UserAnswer = newUserAnswer,
                        CorrectAnswer = correctAnswerForResponse,
                        IsCorrect = isCorrect
                    };
                }
            }

            details.Answers = existing.Values
                .OrderBy(x => allExercises.First(e => e.Id == x.ExerciseId).Order <= 0 ? int.MaxValue : allExercises.First(e => e.Id == x.ExerciseId).Order)
                .ThenBy(x => x.ExerciseId)
                .ToList();

            EnsureDetailsContainsAllExercises(details, allExercises);

            details.MistakeExerciseIds = details.Answers
                .Where(x => !x.IsCorrect)
                .Select(x => x.ExerciseId)
                .Distinct()
                .ToList();

            var correct = details.Answers.Count(x => x.IsCorrect);
            bool completedAllMistakes = details.MistakeExerciseIds.Count == 0;

            lastResult.Score = correct;
            lastResult.TotalQuestions = totalExercises;
            lastResult.MistakesJson = SerializeDetails(details);

            _dbContext.SaveChanges();

            return new SubmitLessonMistakesResponse
            {
                LessonId = lessonId,
                TotalExercises = totalExercises,
                CorrectAnswers = correct,
                IsCompleted = completedAllMistakes,
                MistakeExerciseIds = details.MistakeExerciseIds,
                Answers = details.Answers
            };
        }

        private static LessonResultDetailsJson? ParseDetails(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<LessonResultDetailsJson>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private static string SerializeDetails(LessonResultDetailsJson details)
        {
            return JsonSerializer.Serialize(details, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }

        private static void EnsureDetailsContainsAllExercises(LessonResultDetailsJson details, List<Exercise> exercises)
        {
            if (details.Answers == null)
            {
                details.Answers = new List<LessonAnswerResultDto>();
            }

            var byId = details.Answers.ToDictionary(x => x.ExerciseId, x => x);

            foreach (var ex in exercises)
            {
                if (byId.ContainsKey(ex.Id))
                {
                    continue;
                }

                var correctAnswerForResponse = ex.Type == ExerciseType.Match
                    ? (ex.Data ?? string.Empty)
                    : (ex.CorrectAnswer ?? string.Empty);

                details.Answers.Add(new LessonAnswerResultDto
                {
                    ExerciseId = ex.Id,
                    UserAnswer = string.Empty,
                    CorrectAnswer = correctAnswerForResponse,
                    IsCorrect = false
                });
            }

            details.Answers = details.Answers
                .OrderBy(x => exercises.First(e => e.Id == x.ExerciseId).Order <= 0 ? int.MaxValue : exercises.First(e => e.Id == x.ExerciseId).Order)
                .ThenBy(x => x.ExerciseId)
                .ToList();
        }

        private static ExerciseResponse MapExercise(Exercise ex)
        {
            return new ExerciseResponse
            {
                Id = ex.Id,
                Type = ex.Type.ToString(),
                Question = ex.Question ?? string.Empty,
                Data = ex.Data ?? string.Empty,
                Order = ex.Order
            };
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

            if (correctMap.Count != userMap.Count)
            {
                return false;
            }

            foreach (var kv in correctMap)
            {
                if (!userMap.ContainsKey(kv.Key))
                {
                    return false;
                }

                if (userMap[kv.Key] != kv.Value)
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
