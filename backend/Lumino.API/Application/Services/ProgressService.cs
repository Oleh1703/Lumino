using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;

namespace Lumino.Api.Application.Services
{
    public class ProgressService : IProgressService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly LearningSettings _learningSettings;

        public ProgressService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _learningSettings = learningSettings.Value;
        }

        public UserProgressResponse GetMyProgress(int userId)
        {
            var progress = _dbContext.UserProgresses
                .FirstOrDefault(x => x.UserId == userId);

            int totalLessons = _dbContext.Lessons.Count();
            int totalScenes = _dbContext.Scenes.Count();

            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            int completedDistinctLessons = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .Select(x => x.LessonId)
                .Distinct()
                .Count();

            int completedDistinctScenes = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .Distinct()
                .Count();

            int completionPercent = 0;

            if (totalLessons > 0 && completedDistinctLessons > 0)
            {
                completionPercent = (int)Math.Round((double)completedDistinctLessons * 100 / totalLessons);
            }

            var nowUtc = _dateTimeProvider.UtcNow;


            int totalVocabulary = _dbContext.UserVocabularies
                .Count(x => x.UserId == userId);

            int dueVocabulary = _dbContext.UserVocabularies
                .Count(x => x.UserId == userId && x.NextReviewAt <= nowUtc);

            DateTime? nextVocabularyReviewAt = _dbContext.UserVocabularies
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.NextReviewAt)
                .Select(x => (DateTime?)x.NextReviewAt)
                .FirstOrDefault();


            var passedLessonDatesUtc = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .Select(x => x.CompletedAt)
                .ToList();

            var completedSceneDatesUtc = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.CompletedAt)
                .ToList();

            var studyDatesUtc = passedLessonDatesUtc
                .Concat(completedSceneDatesUtc)
                .ToList();

            var (currentStreak, lastStudyAt) = CalculateCurrentStreak(studyDatesUtc, nowUtc);

            if (progress == null)
            {
                return new UserProgressResponse
                {
                    CompletedLessons = 0,
                    TotalScore = 0,
                    LastUpdatedAt = nowUtc,
                    TotalLessons = totalLessons,
                    CompletedDistinctLessons = completedDistinctLessons,
                    CompletionPercent = completionPercent,
                    CurrentStreakDays = currentStreak,
                    LastStudyAt = lastStudyAt,
                    TotalScenes = totalScenes,
                    CompletedDistinctScenes = completedDistinctScenes,
                    TotalVocabulary = totalVocabulary,
                    DueVocabulary = dueVocabulary,
                    NextVocabularyReviewAt = nextVocabularyReviewAt
                };
            }

            return new UserProgressResponse
            {
                CompletedLessons = progress.CompletedLessons,
                TotalScore = progress.TotalScore,
                LastUpdatedAt = progress.LastUpdatedAt,
                TotalLessons = totalLessons,
                CompletedDistinctLessons = completedDistinctLessons,
                CompletionPercent = completionPercent,
                CurrentStreakDays = currentStreak,
                LastStudyAt = lastStudyAt,
                TotalScenes = totalScenes,
                CompletedDistinctScenes = completedDistinctScenes,
                TotalVocabulary = totalVocabulary,
                DueVocabulary = dueVocabulary,
                NextVocabularyReviewAt = nextVocabularyReviewAt
            };
        }


        public DailyGoalResponse GetMyDailyGoal(int userId)
        {
            var nowUtc = _dateTimeProvider.UtcNow;
            var todayUtc = nowUtc.Date;
            var tomorrowUtc = todayUtc.AddDays(1);

            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            var todayPassedLessons = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    x.CompletedAt >= todayUtc &&
                    x.CompletedAt < tomorrowUtc &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .ToList();

            var todayCompletedScenes = _dbContext.SceneAttempts
                .Where(x =>
                    x.UserId == userId &&
                    x.IsCompleted &&
                    x.CompletedAt >= todayUtc &&
                    x.CompletedAt < tomorrowUtc
                )
                .ToList();

            int todayScore = todayPassedLessons.Sum(x => x.Score) + todayCompletedScenes.Sum(x => x.Score);

            int targetScore = _learningSettings.DailyGoalScoreTarget;

            if (targetScore < 1)
            {
                targetScore = 1;
            }

            int remaining = targetScore - todayScore;

            if (remaining < 0)
            {
                remaining = 0;
            }

            return new DailyGoalResponse
            {
                DateUtc = todayUtc,
                TargetScore = targetScore,
                TodayScore = todayScore,
                RemainingScore = remaining,
                IsGoalMet = todayScore >= targetScore,
                TodayPassedLessons = todayPassedLessons.Select(x => x.LessonId).Distinct().Count(),
                TodayCompletedScenes = todayCompletedScenes.Select(x => x.SceneId).Distinct().Count()
            };
        }

        private static (int streakDays, DateTime? lastStudyAt) CalculateCurrentStreak(List<DateTime> studyCompletedAtUtc, DateTime nowUtc)
        {
            if (studyCompletedAtUtc == null || studyCompletedAtUtc.Count == 0)
            {
                return (0, null);
            }

            var dates = studyCompletedAtUtc
                .Select(x => x.Date)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var lastDate = dates[^1];
            var today = nowUtc.Date;

            if (lastDate < today.AddDays(-1))
            {
                return (0, lastDate);
            }

            int streak = 1;

            for (int i = dates.Count - 2; i >= 0; i--)
            {
                var expected = lastDate.AddDays(-streak);

                if (dates[i] == expected)
                {
                    streak++;
                    continue;
                }

                break;
            }

            return (streak, lastDate);
        }
    }
}
