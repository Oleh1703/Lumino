using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Collections.Generic;

namespace Lumino.Api.Application.Services
{
    public class LessonResultService : ILessonResultService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IAchievementService _achievementService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ISubmitLessonRequestValidator _submitLessonRequestValidator;
        private readonly LearningSettings _learningSettings;

        public LessonResultService(
            LuminoDbContext dbContext,
            IAchievementService achievementService,
            IDateTimeProvider dateTimeProvider,
            ISubmitLessonRequestValidator submitLessonRequestValidator,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _achievementService = achievementService;
            _dateTimeProvider = dateTimeProvider;
            _submitLessonRequestValidator = submitLessonRequestValidator;
            _learningSettings = learningSettings.Value;
        }

        public SubmitLessonResponse SubmitLesson(int userId, SubmitLessonRequest request)
        {
            _submitLessonRequestValidator.Validate(request);

            var lesson = _dbContext.Lessons.FirstOrDefault(x => x.Id == request.LessonId);

            if (lesson == null)
            {
                throw new KeyNotFoundException("Lesson not found");
            }

            var exercises = _dbContext.Exercises
                .Where(x => x.LessonId == lesson.Id)
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

                var isCorrect = !string.IsNullOrWhiteSpace(userAnswerText) &&
                    userAnswerText.Trim().ToLower() ==
                    exercise.CorrectAnswer.Trim().ToLower();

                answers.Add(new LessonAnswerResultDto
                {
                    ExerciseId = exercise.Id,
                    UserAnswer = userAnswerText,
                    CorrectAnswer = exercise.CorrectAnswer,
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

            // CompletedLessons збільшуємо лише при першому PASSED цього уроку
            var shouldIncrementCompletedLessons = isPassed &&
                !_dbContext.LessonResults.Any(x =>
                    x.UserId == userId &&
                    x.LessonId == lesson.Id &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                );

            // зберігаємо деталізацію в MistakesJson (backward-compatible)
            var detailsJson = new LessonResultDetailsJson
            {
                MistakeExerciseIds = mistakeExerciseIds,
                Answers = answers
            };

            var result = new LessonResult
            {
                UserId = userId,
                LessonId = lesson.Id,
                Score = correct,
                TotalQuestions = exercises.Count,
                MistakesJson = JsonSerializer.Serialize(detailsJson),
                CompletedAt = _dateTimeProvider.UtcNow
            };

            _dbContext.LessonResults.Add(result);
            _dbContext.SaveChanges();

            UpdateUserProgress(userId, shouldIncrementCompletedLessons);

            // автододавання слів у Vocabulary після Passed
            AddLessonVocabularyIfNeeded(userId, lesson, exercises, answers, mistakeExerciseIds, isPassed);

            _achievementService.CheckAndGrantAchievements(userId, correct, exercises.Count);

            return new SubmitLessonResponse
            {
                TotalExercises = exercises.Count,
                CorrectAnswers = correct,
                IsPassed = isPassed,
                MistakeExerciseIds = mistakeExerciseIds,
                Answers = answers
            };
        }

        private void UpdateUserProgress(int userId, bool shouldIncrementCompletedLessons)
        {
            var progress = _dbContext.UserProgresses
                .FirstOrDefault(x => x.UserId == userId);

            if (progress == null)
            {
                progress = new UserProgress
                {
                    UserId = userId,
                    CompletedLessons = shouldIncrementCompletedLessons ? 1 : 0,
                    TotalScore = CalculateBestTotalScore(userId),
                    LastUpdatedAt = _dateTimeProvider.UtcNow
                };

                _dbContext.UserProgresses.Add(progress);
            }
            else
            {
                if (shouldIncrementCompletedLessons)
                {
                    progress.CompletedLessons++;
                }

                // TotalScore = сума найкращих результатів по кожному уроку
                progress.TotalScore = CalculateBestTotalScore(userId);
                progress.LastUpdatedAt = _dateTimeProvider.UtcNow;
            }

            _dbContext.SaveChanges();
        }

        private int CalculateBestTotalScore(int userId)
        {
            return _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .GroupBy(x => x.LessonId)
                .Select(g => g.Max(x => x.Score))
                .Sum();
        }

        private void AddLessonVocabularyIfNeeded(
            int userId,
            Lesson lesson,
            List<Exercise> exercises,
            List<LessonAnswerResultDto> answers,
            List<int> mistakeExerciseIds,
            bool isPassed)
        {
            if (!isPassed)
            {
                return;
            }

            var now = _dateTimeProvider.UtcNow;

            var theoryPairs = ExtractPairsFromTheory(lesson.Theory);
            var mistakePairs = ExtractPairsFromMistakes(exercises, answers, mistakeExerciseIds);

            var allPairs = theoryPairs
                .Concat(mistakePairs)
                .Distinct()
                .ToList();

            if (allPairs.Count == 0)
            {
                return;
            }

            var mistakeSet = new HashSet<(string Word, string Translation)>(mistakePairs);

            foreach (var pair in allPairs)
            {
                var word = Normalize(pair.Word);
                var translation = Normalize(pair.Translation);

                if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(translation))
                {
                    continue;
                }

                var item = _dbContext.VocabularyItems
                    .FirstOrDefault(x => x.Word == word && x.Translation == translation);

                if (item == null)
                {
                    item = new VocabularyItem
                    {
                        Word = word,
                        Translation = translation,
                        Example = null
                    };

                    _dbContext.VocabularyItems.Add(item);
                    _dbContext.SaveChanges();
                }

                var userWord = _dbContext.UserVocabularies
                    .FirstOrDefault(x => x.UserId == userId && x.VocabularyItemId == item.Id);

                var isMistake = mistakeSet.Contains(pair);

                if (userWord == null)
                {
                    userWord = new UserVocabulary
                    {
                        UserId = userId,
                        VocabularyItemId = item.Id,
                        AddedAt = now,
                        LastReviewedAt = null,
                        NextReviewAt = isMistake ? now : now.AddDays(1),
                        ReviewCount = 0
                    };

                    _dbContext.UserVocabularies.Add(userWord);
                    continue;
                }

                // якщо слово вже є — і воно було в помилках, робимо його due зараз
                if (isMistake && userWord.NextReviewAt > now)
                {
                    userWord.NextReviewAt = now;
                }
            }

            _dbContext.SaveChanges();
        }

        private static List<(string Word, string Translation)> ExtractPairsFromTheory(string? theory)
        {
            var result = new List<(string Word, string Translation)>();

            if (string.IsNullOrWhiteSpace(theory))
            {
                return result;
            }

            var lines = theory.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var idx = line.IndexOf('=');

                if (idx <= 0)
                {
                    continue;
                }

                var left = line.Substring(0, idx).Trim();
                var right = line.Substring(idx + 1).Trim();

                if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                {
                    continue;
                }

                result.Add((left, right));
            }

            return result;
        }

        private static List<(string Word, string Translation)> ExtractPairsFromMistakes(
            List<Exercise> exercises,
            List<LessonAnswerResultDto> answers,
            List<int> mistakeExerciseIds)
        {
            var result = new List<(string Word, string Translation)>();

            if (mistakeExerciseIds == null || mistakeExerciseIds.Count == 0)
            {
                return result;
            }

            foreach (var exId in mistakeExerciseIds)
            {
                var exercise = exercises.FirstOrDefault(x => x.Id == exId);

                if (exercise == null)
                {
                    continue;
                }

                if (TryExtractPairFromExercise(exercise, out var pair))
                {
                    result.Add(pair);
                }
            }

            return result;
        }

        private static bool TryExtractPairFromExercise(Exercise exercise, out (string Word, string Translation) pair)
        {
            pair = (string.Empty, string.Empty);

            var q = (exercise.Question ?? string.Empty).Trim();
            var correct = (exercise.CorrectAnswer ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(q) || string.IsNullOrWhiteSpace(correct))
            {
                return false;
            }

            // Pattern: "Hello = ?"
            if (q.Contains("= ?"))
            {
                var idx = q.IndexOf('=');

                if (idx > 0)
                {
                    var left = q.Substring(0, idx).Trim();

                    if (!string.IsNullOrWhiteSpace(left))
                    {
                        pair = (left, correct);
                        return true;
                    }
                }
            }

            // Pattern: "Write Ukrainian for: Goodbye"
            if (q.StartsWith("Write Ukrainian for:", StringComparison.OrdinalIgnoreCase))
            {
                var idx = q.IndexOf(':');

                if (idx >= 0 && idx + 1 < q.Length)
                {
                    var word = q.Substring(idx + 1).Trim();

                    if (!string.IsNullOrWhiteSpace(word))
                    {
                        pair = (word, correct);
                        return true;
                    }
                }
            }

            // Pattern: "Write English: У мене все добре"
            if (q.StartsWith("Write English:", StringComparison.OrdinalIgnoreCase))
            {
                var idx = q.IndexOf(':');

                if (idx >= 0 && idx + 1 < q.Length)
                {
                    var translation = q.Substring(idx + 1).Trim();

                    if (!string.IsNullOrWhiteSpace(translation))
                    {
                        pair = (correct, translation);
                        return true;
                    }
                }
            }

            return false;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToLower();
        }
    }
}
