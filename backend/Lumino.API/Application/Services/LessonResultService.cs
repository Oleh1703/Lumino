using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.EntityFrameworkCore;
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
        private readonly IUserEconomyService _userEconomyService;
        private readonly IStreakService _streakService;
        private readonly ISubmitLessonRequestValidator _submitLessonRequestValidator;
        private readonly LearningSettings _learningSettings;

        public LessonResultService(
            LuminoDbContext dbContext,
            IAchievementService achievementService,
            IDateTimeProvider dateTimeProvider,
            IUserEconomyService userEconomyService,
            IStreakService streakService,
            ISubmitLessonRequestValidator submitLessonRequestValidator,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _achievementService = achievementService;
            _dateTimeProvider = dateTimeProvider;
            _userEconomyService = userEconomyService;
            _streakService = streakService;
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

            var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);

            if (idempotencyKey != null)
            {
                var existingResult = _dbContext.LessonResults
                    .FirstOrDefault(x => x.UserId == userId && x.LessonId == lesson.Id && x.IdempotencyKey == idempotencyKey);

                if (existingResult != null)
                {
                    return BuildResponseFromExistingResult(existingResult);
                }
            }

            var exercises = _dbContext.Exercises
                .Where(x => x.LessonId == lesson.Id)
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
                IdempotencyKey = idempotencyKey,
                MistakesJson = JsonSerializer.Serialize(detailsJson),
                CompletedAt = _dateTimeProvider.UtcNow
            };

            _dbContext.LessonResults.Add(result);

            try
            {
                _dbContext.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // якщо паралельно прилетів такий самий submit з тим самим ключем
                if (idempotencyKey != null)
                {
                    var existingResult = _dbContext.LessonResults
                        .FirstOrDefault(x => x.UserId == userId && x.LessonId == lesson.Id && x.IdempotencyKey == idempotencyKey);

                    if (existingResult != null)
                    {
                        return BuildResponseFromExistingResult(existingResult);
                    }
                }

                throw;
            }


            _userEconomyService.ConsumeHeartsForMistakes(userId, mistakeExerciseIds.Count);

            UpdateUserProgress(userId, shouldIncrementCompletedLessons);

            // автододавання слів у Vocabulary після Passed
            AddLessonVocabularyIfNeeded(userId, lesson, exercises, answers, mistakeExerciseIds, isPassed);

            // активний курс + прогрес уроків + unlock наступного + перенос LastLessonId
            UpdateCourseProgressAfterLesson(userId, lesson.Id, isPassed, correct);

            if (isPassed)
            {
                _streakService.RegisterLessonActivity(userId);
            }


            if (shouldIncrementCompletedLessons)
            {
                _userEconomyService.AwardCrystalsForPassedLessonIfNeeded(userId);
            }

            var earnedCrystals = 0;

            if (shouldIncrementCompletedLessons)
            {
                var reward = _learningSettings.CrystalsRewardPerPassedLesson;
                earnedCrystals = reward > 0 ? reward : 0;
            }

            _achievementService.CheckAndGrantAchievements(userId, correct, exercises.Count);

            return new SubmitLessonResponse
            {
                TotalExercises = exercises.Count,
                CorrectAnswers = correct,
                IsPassed = isPassed,
                EarnedCrystals = earnedCrystals,
                MistakeExerciseIds = mistakeExerciseIds,
                Answers = answers
            };
        }

        private SubmitLessonResponse BuildResponseFromExistingResult(LessonResult result)
        {
            var passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);
            var isPassed = LessonPassingRules.IsPassed(result.Score, result.TotalQuestions, passingScorePercent);

            var earnedCrystals = GetEarnedCrystalsForLessonResult(result, passingScorePercent);

            var mistakeExerciseIds = new List<int>();
            var answers = new List<LessonAnswerResultDto>();

            if (!string.IsNullOrWhiteSpace(result.MistakesJson))
            {
                try
                {
                    var details = JsonSerializer.Deserialize<LessonResultDetailsJson>(result.MistakesJson);

                    if (details != null)
                    {
                        mistakeExerciseIds = details.MistakeExerciseIds ?? new List<int>();
                        answers = details.Answers ?? new List<LessonAnswerResultDto>();
                    }
                }
                catch
                {
                    // ignore broken json - keep empty arrays (backward-compatible)
                }
            }

            return new SubmitLessonResponse
            {
                TotalExercises = result.TotalQuestions,
                CorrectAnswers = result.Score,
                IsPassed = isPassed,
                EarnedCrystals = earnedCrystals,
                MistakeExerciseIds = mistakeExerciseIds,
                Answers = answers
            };
        }

        private int GetEarnedCrystalsForLessonResult(LessonResult result, int passingScorePercent)
        {
            var reward = _learningSettings.CrystalsRewardPerPassedLesson;

            if (reward <= 0)
            {
                return 0;
            }

            var isPassed = LessonPassingRules.IsPassed(result.Score, result.TotalQuestions, passingScorePercent);

            if (!isPassed)
            {
                return 0;
            }

            var firstPassed = _dbContext.LessonResults
                .Where(x => x.UserId == result.UserId && x.LessonId == result.LessonId && x.TotalQuestions > 0)
                .AsEnumerable()
                .Where(x => LessonPassingRules.IsPassed(x.Score, x.TotalQuestions, passingScorePercent))
                .OrderBy(x => x.CompletedAt)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            if (firstPassed == null)
            {
                return 0;
            }

            return firstPassed.Id == result.Id ? reward : 0;
        }

        private string? NormalizeIdempotencyKey(string? key)
        {
            if (key == null)
            {
                return null;
            }

            var normalized = key.Trim();

            return normalized.Length == 0 ? null : normalized;
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
            var orderedLessons =
                (from t in _dbContext.Topics
                 join l in _dbContext.Lessons on t.Id equals l.TopicId
                 where t.CourseId == courseId
                 orderby (t.Order <= 0 ? int.MaxValue : t.Order), t.Id, (l.Order <= 0 ? int.MaxValue : l.Order), l.Id
                 select new
                 {
                     LessonId = l.Id,
                     TopicId = t.Id
                 })
                .ToList();

            if (orderedLessons.Count == 0)
            {
                return null;
            }

            int index = orderedLessons.FindIndex(x => x.LessonId == currentLessonId);

            if (index < 0 || index + 1 >= orderedLessons.Count)
            {
                return null;
            }

            var current = orderedLessons[index];
            var nextInfo = orderedLessons[index + 1];

            if (nextInfo.TopicId != current.TopicId)
            {
                bool hasCurrentTopicScene = _dbContext.Scenes.Any(x => x.TopicId == current.TopicId);

                if (hasCurrentTopicScene)
                {
                    bool isCurrentTopicSceneCompleted = _dbContext.SceneAttempts
                        .Where(x => x.UserId == userId && x.IsCompleted)
                        .Join(_dbContext.Scenes,
                            attempt => attempt.SceneId,
                            scene => scene.Id,
                            (attempt, scene) => scene)
                        .Any(x => x.TopicId == current.TopicId);

                    if (!isCurrentTopicSceneCompleted)
                    {
                        return null;
                    }
                }
            }

            int nextLessonId = nextInfo.LessonId;

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

            if (completedSet.Count < lessonIds.Count)
            {
                return;
            }

            var courseSceneIds = _dbContext.Scenes
                .Where(x => x.CourseId == courseId)
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

                foreach (var attempt in _dbContext.SceneAttempts.Local)
                {
                    if (attempt.UserId != userId)
                    {
                        continue;
                    }

                    if (!courseSceneIds.Contains(attempt.SceneId))
                    {
                        continue;
                    }

                    if (attempt.IsCompleted)
                    {
                        completedSceneIds.Add(attempt.SceneId);
                    }
                    else
                    {
                        completedSceneIds.Remove(attempt.SceneId);
                    }
                }

                if (completedSceneIds.Count < courseSceneIds.Count)
                {
                    return;
                }
            }

            userCourse.IsCompleted = true;

            if (userCourse.CompletedAt == null)
            {
                userCourse.CompletedAt = now;
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
            var correctRaw = (exercise.CorrectAnswer ?? string.Empty);
            var correct = (ParseCorrectAnswers(correctRaw).FirstOrDefault() ?? string.Empty).Trim();

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
