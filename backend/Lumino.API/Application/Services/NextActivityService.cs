using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
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

            var passedLessonIds = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .Select(x => x.LessonId)
                .Distinct()
                .ToList();

            // якщо є активний курс — шукаємо next lesson в межах активного курсу,
            // якщо активного курсу нема — fallback.
            var activeCourse = _dbContext.UserCourses
                .FirstOrDefault(x => x.UserId == userId && x.IsActive);

            if (activeCourse != null)
            {
                var lessonsInCourse = (
                    from t in _dbContext.Topics
                    join l in _dbContext.Lessons on t.Id equals l.TopicId
                    join c in _dbContext.Courses on t.CourseId equals c.Id
                    where c.IsPublished && c.Id == activeCourse.CourseId
                    orderby t.Order, l.Order
                    select new
                    {
                        LessonId = l.Id,
                        TopicId = t.Id,
                        LessonTitle = l.Title
                    }
                ).ToList();

                if (lessonsInCourse.Count > 0)
                {
                    var progressDict = _dbContext.UserLessonProgresses
                        .Where(x => x.UserId == userId)
                        .ToDictionary(x => x.LessonId, x => x);

                    // 1) спробувати повернути LastLessonId (якщо він ще не пройдений)
                    if (activeCourse.LastLessonId != null)
                    {
                        var lastId = activeCourse.LastLessonId.Value;

                        if (!passedLessonIds.Contains(lastId))
                        {
                            if (progressDict.TryGetValue(lastId, out var lp))
                            {
                                if (lp.IsUnlocked)
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
                            else
                            {
                                // якщо прогресу ще нема (дуже рідко), не ламаємось — просто віддамо як next в курсі
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

                    // 2) перший unlocked і непройдений урок у рамках активного курсу
                    foreach (var item in lessonsInCourse)
                    {
                        if (passedLessonIds.Contains(item.LessonId))
                        {
                            continue;
                        }

                        if (progressDict.TryGetValue(item.LessonId, out var p))
                        {
                            if (!p.IsUnlocked)
                            {
                                continue;
                            }

                            return new NextActivityResponse
                            {
                                Type = "Lesson",
                                LessonId = item.LessonId,
                                TopicId = item.TopicId,
                                LessonTitle = item.LessonTitle
                            };
                        }
                    }

                    // fallback в рамках активного курсу (якщо прогреси ще не створені/порожні)
                    var firstNotPassed = lessonsInCourse
                        .FirstOrDefault(x => !passedLessonIds.Contains(x.LessonId));

                    if (firstNotPassed != null)
                    {
                        return new NextActivityResponse
                        {
                            Type = "Lesson",
                            LessonId = firstNotPassed.LessonId,
                            TopicId = firstNotPassed.TopicId,
                            LessonTitle = firstNotPassed.LessonTitle
                        };
                    }
                }
            }

            // fallback
            var nextLesson =
                (from l in _dbContext.Lessons
                 join t in _dbContext.Topics on l.TopicId equals t.Id
                 join c in _dbContext.Courses on t.CourseId equals c.Id
                 where c.IsPublished
                 orderby c.Id, t.Order, l.Order
                 select new
                 {
                     LessonId = l.Id,
                     TopicId = t.Id,
                     LessonTitle = l.Title
                 })
                .AsEnumerable()
                .FirstOrDefault(x => !passedLessonIds.Contains(x.LessonId));

            if (nextLesson == null)
            {
                return null;
            }

            return new NextActivityResponse
            {
                Type = "Lesson",
                LessonId = nextLesson.LessonId,
                TopicId = nextLesson.TopicId,
                LessonTitle = nextLesson.LessonTitle
            };
        }

        private NextActivityResponse? GetNextScene(int userId)
        {
            var completedSceneIds = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .Distinct()
                .ToList();

            var scene = _dbContext.Scenes
                .OrderBy(x => x.Id)
                .AsEnumerable()
                .FirstOrDefault(x => !completedSceneIds.Contains(x.Id));

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
