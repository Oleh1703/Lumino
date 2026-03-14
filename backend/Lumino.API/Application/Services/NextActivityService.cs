using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;

namespace Lumino.Api.Application.Services
{
    public class NextActivityService : INextActivityService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly LearningSettings _learningSettings;

        public NextActivityService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _learningSettings = learningSettings.Value;
        }

        public NextActivityResponse? GetNext(int userId)
        {
            var now = _dateTimeProvider.UtcNow;

            var nextVocab = GetNextDueVocabulary(userId, now);

            if (nextVocab != null)
            {
                return nextVocab;
            }

            var nextLesson = GetNextLesson(userId);

            if (nextLesson != null)
            {
                return nextLesson;
            }

            var nextScene = GetNextScene(userId);

            if (nextScene != null)
            {
                return nextScene;
            }

            // ✅ доробка №17: якщо немає next lesson/scene і немає due-review — курс завершено
            var activeCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.IsActive);

            int? courseIdToUse = activeCourse?.CourseId;

            if (courseIdToUse == null)
            {
                courseIdToUse = GetFirstPublishedCourseWithLessonsId();

                if (courseIdToUse == null)
                {
                    return null;
                }
            }

            return new NextActivityResponse
            {
                Type = "CourseComplete",
                CourseId = courseIdToUse.Value,
                IsLocked = false
            };
        }

        private NextActivityResponse? GetNextDueVocabulary(int userId, DateTime nowUtc)
        {
            var uv = _dbContext.UserVocabularies
                .Where(x => x.UserId == userId && x.NextReviewAt <= nowUtc)
                .OrderBy(x => x.NextReviewAt)
                .ThenBy(x => x.AddedAt)
                .FirstOrDefault();

            if (uv == null)
            {
                return null;
            }

            var item = _dbContext.VocabularyItems.FirstOrDefault(x => x.Id == uv.VocabularyItemId);

            if (item == null)
            {
                return null;
            }

            return new NextActivityResponse
            {
                Type = "VocabularyReview",
                CourseId = null,
                IsLocked = false,
                UserVocabularyId = uv.Id,
                VocabularyItemId = item.Id,
                Word = item.Word,
                Translation = item.Translation
            };
        }

        private NextActivityResponse? GetNextLesson(int userId)
        {
            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            var activeCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.IsActive);

            int? courseIdToUse = activeCourse?.CourseId;

            if (courseIdToUse == null)
            {
                courseIdToUse = GetFirstPublishedCourseWithLessonsId();

                if (courseIdToUse == null)
                {
                    return null;
                }
            }

            // формуємо список уроків курсу в правильному порядку
            var lessonsInCourse = (
                from t in _dbContext.Topics
                join l in _dbContext.Lessons on t.Id equals l.TopicId
                join c in _dbContext.Courses on t.CourseId equals c.Id
                where c.IsPublished && c.Id == courseIdToUse.Value
                orderby (t.Order <= 0 ? int.MaxValue : t.Order), t.Id, (l.Order <= 0 ? int.MaxValue : l.Order), l.Id
                select new CourseLessonInfo
                {
                    LessonId = l.Id,
                    TopicId = t.Id,
                    LessonTitle = l.Title
                }
            ).ToList();

            if (lessonsInCourse.Count == 0)
            {
                return null;
            }

            // passed визначаємо по LessonResults (80%+)
            var passedLessonIds = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .Select(x => x.LessonId)
                .Distinct()
                .ToList();

            // перед вибором next уроку гарантуємо, що прогрес у БД синхронізований
            EnsureUserLessonProgressForCourse(userId, courseIdToUse.Value, lessonsInCourse, passedLessonIds);

            var progressDict = _dbContext.UserLessonProgresses
                .Where(x => x.UserId == userId)
                .ToDictionary(x => x.LessonId, x => x);

            // 1) якщо є активний курс і LastLessonId ще не пройдений — повертаємо його ТІЛЬКИ якщо unlocked
            if (activeCourse != null && activeCourse.LastLessonId != null)
            {
                var lastId = activeCourse.LastLessonId.Value;

                if (!passedLessonIds.Contains(lastId))
                {
                    if (progressDict.TryGetValue(lastId, out var lp) && lp.IsUnlocked)
                    {
                        var lastLesson = lessonsInCourse.FirstOrDefault(x => x.LessonId == lastId);

                        if (lastLesson != null)
                        {
                            return new NextActivityResponse
                            {
                                Type = "Lesson",
                                CourseId = courseIdToUse.Value,
                                IsLocked = false,
                                LessonId = lastLesson.LessonId,
                                TopicId = lastLesson.TopicId,
                                LessonTitle = lastLesson.LessonTitle
                            };
                        }
                    }
                }
            }

            // 2) перший unlocked і непройдений урок у курсі
            foreach (var item in lessonsInCourse)
            {
                if (passedLessonIds.Contains(item.LessonId))
                {
                    continue;
                }

                if (progressDict.TryGetValue(item.LessonId, out var p) && p.IsUnlocked)
                {
                    return new NextActivityResponse
                    {
                        Type = "Lesson",
                        CourseId = courseIdToUse.Value,
                        IsLocked = false,
                        LessonId = item.LessonId,
                        TopicId = item.TopicId,
                        LessonTitle = item.LessonTitle
                    };
                }
            }

            return null;
        }

        private int? GetFirstPublishedCourseWithLessonsId()
        {
            var courseId = (
                from c in _dbContext.Courses
                join t in _dbContext.Topics on c.Id equals t.CourseId
                join l in _dbContext.Lessons on t.Id equals l.TopicId
                where c.IsPublished
                orderby c.Id
                select (int?)c.Id
            ).FirstOrDefault();

            return courseId;
        }

        private void EnsureUserLessonProgressForCourse(
            int userId,
            int courseId,
            List<CourseLessonInfo> orderedLessons,
            List<int> passedLessonIds)
        {
            if (orderedLessons == null || orderedLessons.Count == 0)
            {
                return;
            }

            var lessonIds = orderedLessons.Select(x => x.LessonId).ToList();

            var existing = _dbContext.UserLessonProgresses
                .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonId))
                .ToDictionary(x => x.LessonId, x => x);

            // best score підтягнемо з LessonResults 
            var bestScores = _dbContext.LessonResults
                .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonId))
                .GroupBy(x => x.LessonId)
                .Select(g => new
                {
                    LessonId = g.Key,
                    BestScore = g.Max(x => x.Score)
                })
                .ToDictionary(x => x.LessonId, x => x.BestScore);

            bool needSave = false;
            var lessonTopicIds = orderedLessons
                .ToDictionary(x => x.LessonId, x => x.TopicId);

            var completedSceneTopicIds = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Join(_dbContext.Scenes,
                    attempt => attempt.SceneId,
                    scene => scene.Id,
                    (attempt, scene) => scene.TopicId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToHashSet();

            for (int i = 0; i < orderedLessons.Count; i++)
            {
                int lessonId = orderedLessons[i].LessonId;

                bool shouldBeUnlocked = i == 0 || IsLessonUnlockedByCourseFlow(
                    orderedLessons,
                    i,
                    existing,
                    lessonTopicIds,
                    completedSceneTopicIds);
                bool isPassed = passedLessonIds.Contains(lessonId);

                int bestScore = 0;

                if (bestScores.TryGetValue(lessonId, out var bs))
                {
                    bestScore = bs;
                }

                if (!existing.TryGetValue(lessonId, out var p))
                {
                    p = new UserLessonProgress
                    {
                        UserId = userId,
                        LessonId = lessonId,
                        IsUnlocked = shouldBeUnlocked,
                        IsCompleted = isPassed,
                        BestScore = bestScore,
                        LastAttemptAt = null
                    };

                    _dbContext.UserLessonProgresses.Add(p);
                    existing[lessonId] = p;
                    needSave = true;
                }
                else
                {
                    if (shouldBeUnlocked && !p.IsUnlocked)
                    {
                        p.IsUnlocked = true;
                        needSave = true;
                    }

                    if (isPassed && !p.IsCompleted)
                    {
                        p.IsCompleted = true;
                        needSave = true;
                    }

                    if (bestScore > p.BestScore)
                    {
                        p.BestScore = bestScore;
                        needSave = true;
                    }
                }

            }

            if (needSave)
            {
                _dbContext.SaveChanges();
            }
        }


        private bool IsLessonUnlockedByCourseFlow(
            List<CourseLessonInfo> orderedLessons,
            int currentIndex,
            Dictionary<int, UserLessonProgress> existing,
            Dictionary<int, int> lessonTopicIds,
            HashSet<int> completedSceneTopicIds)
        {
            if (currentIndex <= 0)
            {
                return true;
            }

            var previousLessonId = orderedLessons[currentIndex - 1].LessonId;
            var currentLessonId = orderedLessons[currentIndex].LessonId;

            if (!existing.TryGetValue(previousLessonId, out var previousProgress) || !previousProgress.IsCompleted)
            {
                return false;
            }

            var previousTopicId = lessonTopicIds[previousLessonId];
            var currentTopicId = lessonTopicIds[currentLessonId];

            if (previousTopicId == currentTopicId)
            {
                return true;
            }

            return IsTopicGatewayCompleted(orderedLessons, existing, lessonTopicIds, completedSceneTopicIds, previousTopicId);
        }

        private bool IsTopicGatewayCompleted(
            List<CourseLessonInfo> orderedLessons,
            Dictionary<int, UserLessonProgress> existing,
            Dictionary<int, int> lessonTopicIds,
            HashSet<int> completedSceneTopicIds,
            int topicId)
        {
            var topicLessonIds = orderedLessons
                .Select(x => x.LessonId)
                .Where(x => lessonTopicIds.ContainsKey(x) && lessonTopicIds[x] == topicId)
                .Distinct()
                .ToList();

            if (topicLessonIds.Count == 0)
            {
                return true;
            }

            bool allLessonsCompleted = topicLessonIds.All(x => existing.ContainsKey(x) && existing[x].IsCompleted);

            if (!allLessonsCompleted)
            {
                return false;
            }

            bool hasTopicScene = _dbContext.Scenes.Any(x => x.TopicId == topicId);

            if (!hasTopicScene)
            {
                return true;
            }

            return completedSceneTopicIds.Contains(topicId);
        }

        private class CourseLessonInfo
        {
            public int LessonId { get; set; }
            public int TopicId { get; set; }
            public string LessonTitle { get; set; } = "";
        }

        private NextActivityResponse? GetNextScene(int userId)
        {
            var activeCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.IsActive);

            int? courseIdToUse = activeCourse?.CourseId;

            if (courseIdToUse == null)
            {
                courseIdToUse = GetFirstPublishedCourseWithLessonsId();

                if (courseIdToUse == null)
                {
                    return null;
                }
            }

            var completedSceneIds = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .Distinct()
                .ToList();

            // unlock-rule для сцен: залежить від кількості passed уроків В АКТИВНОМУ КУРСІ
            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            var lessonIdsInCourse =
                (from t in _dbContext.Topics
                 join l in _dbContext.Lessons on t.Id equals l.TopicId
                 where t.CourseId == courseIdToUse.Value
                 select l.Id)
                .Distinct();

            int passedLessons = _dbContext.LessonResults
                .Where(x => x.UserId == userId && x.TotalQuestions > 0)
                .Where(x => lessonIdsInCourse.Contains(x.LessonId))
                .Where(x => x.Score * 100 >= x.TotalQuestions * passingScorePercent)
                .Select(x => x.LessonId)
                .Distinct()
                .Count();

            var unlockEvery = SceneUnlockRules.NormalizeUnlockEveryLessons(_learningSettings.SceneUnlockEveryLessons);

            // якщо вже є сцени прив'язані до курсу - показуємо тільки їх
            // інакше (legacy) - беремо сцени без CourseId
            bool hasCourseScenes = _dbContext.Scenes.Any(x => x.CourseId == courseIdToUse.Value);

            var scenesQuery = _dbContext.Scenes.AsQueryable();

            if (hasCourseScenes)
            {
                scenesQuery = scenesQuery.Where(x => x.CourseId == courseIdToUse.Value);
            }
            else
            {
                scenesQuery = scenesQuery.Where(x => x.CourseId == null);
            }

            var orderedScenes = scenesQuery
                .AsEnumerable()
                .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            Scene? scene = null;

            for (int i = 0; i < orderedScenes.Count; i++)
            {
                var s = orderedScenes[i];

                if (completedSceneIds.Contains(s.Id))
                {
                    continue;
                }

                int scenePosition = i + 1;

                bool isUnlocked;

                if (s.TopicId.HasValue)
                {
                    var stats = GetTopicLessonStats(userId, s.TopicId.Value, passingScorePercent);
                    isUnlocked = stats.PassedLessons >= stats.TotalLessons;
                }
                else
                {
                    isUnlocked = SceneUnlockRules.IsUnlocked(scenePosition, passedLessons, unlockEvery);
                }

                if (isUnlocked)
                {
                    scene = s;
                    break;
                }
            }

            if (scene == null)
            {
                return null;
            }

            return new NextActivityResponse
            {
                Type = "Scene",
                CourseId = courseIdToUse.Value,
                IsLocked = false,
                SceneId = scene.Id,
                SceneTitle = scene.Title,
                TopicId = scene.TopicId
            };
        }


        private TopicLessonStats GetTopicLessonStats(int userId, int topicId, int passingScorePercent)
        {
            var lessonIds = _dbContext.Lessons
                .Where(x => x.TopicId == topicId)
                .Select(x => x.Id)
                .ToList();

            if (lessonIds.Count == 0)
            {
                return new TopicLessonStats
                {
                    TotalLessons = 0,
                    PassedLessons = 0
                };
            }

            var passedLessonIds = _dbContext.LessonResults
                .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonId) && x.TotalQuestions > 0)
                .AsEnumerable()
                .Where(x => x.Score * 100 >= x.TotalQuestions * passingScorePercent)
                .Select(x => x.LessonId)
                .Distinct()
                .ToHashSet();

            return new TopicLessonStats
            {
                TotalLessons = lessonIds.Count,
                PassedLessons = passedLessonIds.Count
            };
        }

        private class TopicLessonStats
        {
            public int TotalLessons { get; set; }

            public int PassedLessons { get; set; }
        }
    }
}
