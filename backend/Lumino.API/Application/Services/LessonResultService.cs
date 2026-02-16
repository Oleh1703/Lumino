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

            var progress = _dbContext.UserLessonProgresses
                .FirstOrDefault(x => x.UserId == userId && x.LessonId == lesson.Id);

            if (progress == null || !progress.IsUnlocked)
            {
                throw new ForbiddenAccessException("Lesson is locked");
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

            // активний курс + прогрес уроків + unlock наступного + перенос LastLessonId
            UpdateCourseProgressAfterLesson(userId, lesson.Id, isPassed, correct);

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
            var results = _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .ToList();

            if (results.Count == 0)
            {
                return 0;
            }

            var bestByLesson = new Dictionary<int, int>();

            foreach (var r in results)
            {
                if (!bestByLesson.ContainsKey(r.LessonId))
                {
                    bestByLesson[r.LessonId] = r.Score;
                    continue;
                }

                if (r.Score > bestByLesson[r.LessonId])
                {
                    bestByLesson[r.LessonId] = r.Score;
                }
            }

            return bestByLesson.Values.Sum();
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

            var lessonVocabItemIds = _dbContext.LessonVocabularies
                .Where(x => x.LessonId == lesson.Id)
                .Select(x => x.VocabularyItemId)
                .Distinct()
                .ToList();

            var theoryPairs = lessonVocabItemIds.Count > 0
                ? _dbContext.VocabularyItems
                    .Where(x => lessonVocabItemIds.Contains(x.Id))
                    .ToList()
                    .Select(x => (x.Word, x.Translation))
                    .ToList()
                : ExtractPairsFromTheory(lesson.Theory);

            var mistakeExerciseVocabItemIds = _dbContext.ExerciseVocabularies
                .Where(x => mistakeExerciseIds.Contains(x.ExerciseId))
                .Select(x => x.VocabularyItemId)
                .Distinct()
                .ToList();

            var mistakePairs = mistakeExerciseVocabItemIds.Count > 0
                ? _dbContext.VocabularyItems
                    .Where(x => mistakeExerciseVocabItemIds.Contains(x.Id))
                    .ToList()
                    .Select(x => (x.Word, x.Translation))
                    .ToList()
                : ExtractPairsFromMistakes(exercises, answers, mistakeExerciseIds);

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

        // курс визначаємо через Lesson -> Topic -> Course
        private void UpdateCourseProgressAfterLesson(int userId, int lessonId, bool isPassed, int score)
        {
            var now = _dateTimeProvider.UtcNow;

            var topicId = _dbContext.Lessons
                .Where(x => x.Id == lessonId)
                .Select(x => (int?)x.TopicId)
                .FirstOrDefault();

            if (topicId == null)
            {
                return;
            }

            var courseId = _dbContext.Topics
                .Where(x => x.Id == topicId.Value)
                .Select(x => (int?)x.CourseId)
                .FirstOrDefault();

            if (courseId == null)
            {
                return;
            }

            var userCourse = EnsureActiveCourse(userId, courseId.Value, lessonId, now);

            UpsertLessonProgress(userId, lessonId, isPassed, score, now);

            int? nextLessonId = null;

            if (isPassed)
            {
                nextLessonId = UnlockNextLesson(userId, courseId.Value, lessonId, now);

                // після Passed переносимо "де продовжувати" на наступний урок
                if (userCourse != null && nextLessonId != null)
                {
                    userCourse.LastLessonId = nextLessonId.Value;
                    userCourse.LastOpenedAt = now;
                }

                TryMarkCourseCompleted(userId, userCourse, courseId.Value, now);
            }

            _dbContext.SaveChanges();
        }

        private UserCourse? EnsureActiveCourse(int userId, int courseId, int lastLessonId, DateTime now)
        {
            var activeOther = _dbContext.UserCourses
                .Where(x => x.UserId == userId && x.IsActive && x.CourseId != courseId)
                .ToList();

            foreach (var item in activeOther)
            {
                item.IsActive = false;
                item.LastOpenedAt = now;
            }

            var userCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.CourseId == courseId);

            if (userCourse == null)
            {
                userCourse = new UserCourse
                {
                    UserId = userId,
                    CourseId = courseId,
                    IsActive = true,
                    LastLessonId = lastLessonId,
                    StartedAt = now,
                    LastOpenedAt = now
                };

                _dbContext.UserCourses.Add(userCourse);
                return userCourse;
            }

            userCourse.IsActive = true;
            userCourse.LastLessonId = lastLessonId;
            userCourse.LastOpenedAt = now;

            if (userCourse.StartedAt == default)
            {
                userCourse.StartedAt = now;
            }

            return userCourse;
        }

        private void UpsertLessonProgress(int userId, int lessonId, bool isPassed, int score, DateTime now)
        {
            var progress = _dbContext.UserLessonProgresses
                .FirstOrDefault(x => x.UserId == userId && x.LessonId == lessonId);

            if (progress == null)
            {
                progress = new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = lessonId,
                    IsUnlocked = true,
                    IsCompleted = isPassed,
                    BestScore = score,
                    LastAttemptAt = now
                };

                _dbContext.UserLessonProgresses.Add(progress);
                return;
            }

            if (!progress.IsUnlocked)
            {
                progress.IsUnlocked = true;
            }

            if (score > progress.BestScore)
            {
                progress.BestScore = score;
            }

            if (isPassed)
            {
                progress.IsCompleted = true;
            }

            progress.LastAttemptAt = now;
        }

        private int? UnlockNextLesson(int userId, int courseId, int currentLessonId, DateTime now)
        {
            var orderedLessonIds =
                (from t in _dbContext.Topics
                 join l in _dbContext.Lessons on t.Id equals l.TopicId
                 where t.CourseId == courseId
                 orderby (t.Order > 0 ? t.Order : t.Id), (l.Order > 0 ? l.Order : l.Id)
                 select l.Id)
                .ToList();

            if (orderedLessonIds.Count == 0)
            {
                return null;
            }

            int index = orderedLessonIds.IndexOf(currentLessonId);

            if (index < 0 || index + 1 >= orderedLessonIds.Count)
            {
                return null;
            }

            int nextLessonId = orderedLessonIds[index + 1];

            var next = _dbContext.UserLessonProgresses
                .FirstOrDefault(x => x.UserId == userId && x.LessonId == nextLessonId);

            if (next == null)
            {
                next = new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = nextLessonId,
                    IsUnlocked = true,
                    IsCompleted = false,
                    BestScore = 0,
                    LastAttemptAt = now
                };

                _dbContext.UserLessonProgresses.Add(next);
                return nextLessonId;
            }

            if (!next.IsUnlocked)
            {
                next.IsUnlocked = true;

                if (next.LastAttemptAt == null)
                {
                    next.LastAttemptAt = now;
                }
            }

            return nextLessonId;
        }


        private void TryMarkCourseCompleted(int userId, UserCourse? userCourse, int courseId, DateTime now)
        {
            if (userCourse == null)
            {
                return;
            }

            if (userCourse.IsCompleted)
            {
                return;
            }

            var lessonIds =
                (from t in _dbContext.Topics
                 join l in _dbContext.Lessons on t.Id equals l.TopicId
                 where t.CourseId == courseId
                 select l.Id)
                .Distinct()
                .ToList();

            if (lessonIds.Count == 0)
            {
                return;
            }

            // Враховуємо завершені уроки з БД
            var completedLessonIds = _dbContext.UserLessonProgresses
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Where(x => lessonIds.Contains(x.LessonId))
                .Select(x => x.LessonId)
                .Distinct()
                .ToList();

            var completedSet = new HashSet<int>(completedLessonIds);

            // Враховуємо зміни в поточному DbContext (до SaveChanges),
            // щоб поточний PASSED урок також врахувався при завершенні курсу
            foreach (var progress in _dbContext.UserLessonProgresses.Local)
            {
                if (progress.UserId != userId)
                {
                    continue;
                }

                if (!lessonIds.Contains(progress.LessonId))
                {
                    continue;
                }

                if (progress.IsCompleted)
                {
                    completedSet.Add(progress.LessonId);
                }
                else
                {
                    completedSet.Remove(progress.LessonId);
                }
            }

            if (completedSet.Count >= lessonIds.Count)
            {
                userCourse.IsCompleted = true;

                if (userCourse.CompletedAt == null)
                {
                    userCourse.CompletedAt = now;
                }
            }
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
    }
}
