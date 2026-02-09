using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class ProgressService : IProgressService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ProgressService(LuminoDbContext dbContext, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
        }

        public UserProgressResponse GetMyProgress(int userId)
        {
            var progress = _dbContext.UserProgresses
                .FirstOrDefault(x => x.UserId == userId);

            int totalLessons = _dbContext.Lessons.Count();

            int completedDistinctLessons = _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .Select(x => x.LessonId)
                .Distinct()
                .Count();

            int completionPercent = 0;

            if (totalLessons > 0 && completedDistinctLessons > 0)
            {
                completionPercent = (int)Math.Round((double)completedDistinctLessons * 100 / totalLessons);
            }

            var nowUtc = _dateTimeProvider.UtcNow;

            var studyDatesUtc = _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .Select(x => x.CompletedAt)
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
                    LastStudyAt = lastStudyAt
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
                LastStudyAt = lastStudyAt
            };
        }

        private static (int streakDays, DateTime? lastStudyAt) CalculateCurrentStreak(List<DateTime> lessonCompletedAtUtc, DateTime nowUtc)
        {
            if (lessonCompletedAtUtc == null || lessonCompletedAtUtc.Count == 0)
            {
                return (0, null);
            }

            var dates = lessonCompletedAtUtc
                .Select(x => x.Date)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var lastDate = dates[^1];
            var today = nowUtc.Date;

            // Якщо останнє навчання було раніше ніж вчора — поточний streak = 0
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
