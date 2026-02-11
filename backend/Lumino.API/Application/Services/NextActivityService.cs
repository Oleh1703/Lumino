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
