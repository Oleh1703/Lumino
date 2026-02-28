using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class ProfileService : IProfileService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ProfileService(LuminoDbContext dbContext, IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
        }

        public WeeklyProgressResponse GetWeeklyProgress(int userId)
        {
            var todayUtc = _dateTimeProvider.UtcNow.Date;

            // Monday as start of week
            int offset = ((int)todayUtc.DayOfWeek + 6) % 7;
            var currentWeekStart = todayUtc.AddDays(-offset);
            var currentWeekEnd = currentWeekStart.AddDays(6);

            var previousWeekStart = currentWeekStart.AddDays(-7);
            var previousWeekEnd = previousWeekStart.AddDays(6);

            var currentWeekPoints = _dbContext.LessonResults
                .Where(x => x.UserId == userId && x.CompletedAt.Date >= currentWeekStart && x.CompletedAt.Date <= currentWeekEnd)
                .GroupBy(x => x.CompletedAt.Date)
                .Select(g => new { Date = g.Key, Points = g.Sum(x => x.Score) })
                .ToDictionary(x => x.Date, x => x.Points);

            var previousWeekPoints = _dbContext.LessonResults
                .Where(x => x.UserId == userId && x.CompletedAt.Date >= previousWeekStart && x.CompletedAt.Date <= previousWeekEnd)
                .GroupBy(x => x.CompletedAt.Date)
                .Select(g => new { Date = g.Key, Points = g.Sum(x => x.Score) })
                .ToDictionary(x => x.Date, x => x.Points);

            var result = new WeeklyProgressResponse
            {
                TotalPoints = _dbContext.LessonResults.Where(x => x.UserId == userId).Sum(x => (int?)x.Score) ?? 0
            };

            for (var i = 0; i < 7; i++)
            {
                var date = currentWeekStart.AddDays(i);
                result.CurrentWeek.Add(new WeeklyProgressDayResponse
                {
                    DateUtc = date,
                    Points = currentWeekPoints.TryGetValue(date, out var p) ? p : 0
                });
            }

            for (var i = 0; i < 7; i++)
            {
                var date = previousWeekStart.AddDays(i);
                result.PreviousWeek.Add(new WeeklyProgressDayResponse
                {
                    DateUtc = date,
                    Points = previousWeekPoints.TryGetValue(date, out var p) ? p : 0
                });
            }

            return result;
        }
    }
}
