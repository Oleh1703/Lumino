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

            return null;
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
                orderby t.Order, l.Order
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
            bool previousCompleted = false;

            for (int i = 0; i < orderedLessons.Count; i++)
            {
                int lessonId = orderedLessons[i].LessonId;

                bool shouldBeUnlocked = i == 0 || previousCompleted;
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

                previousCompleted = p.IsCompleted;
            }

            if (needSave)
            {
                _dbContext.SaveChanges();
            }
        }

        private class CourseLessonInfo
        {
            public int LessonId { get; set; }
            public int TopicId { get; set; }
            public string LessonTitle { get; set; } = "";
        }

        private NextActivityResponse? GetNextScene(int userId)
        {
            var completedSceneIds = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .Distinct()
                .ToList();

            // unlock-rule для сцен: залежить від кількості passed уроків
            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            int passedLessons = _dbContext.LessonResults
                .Where(x => x.UserId == userId && x.TotalQuestions > 0)
                .Where(x => x.Score * 100 >= x.TotalQuestions * passingScorePercent)
                .Select(x => x.LessonId)
                .Distinct()
                .Count();

            var unlockEvery = SceneUnlockRules.NormalizeUnlockEveryLessons(_learningSettings.SceneUnlockEveryLessons);

            var scene = _dbContext.Scenes
                .OrderBy(x => x.Id)
                .AsEnumerable()
                .FirstOrDefault(x =>
                    !completedSceneIds.Contains(x.Id) &&
                    SceneUnlockRules.IsUnlocked(x.Id, passedLessons, unlockEvery));

            if (scene == null)
            {
                return null;
            }

            return new NextActivityResponse
            {
                Type = "Scene",
                SceneId = scene.Id,
                SceneTitle = scene.Title
            };
        }
    }
}
