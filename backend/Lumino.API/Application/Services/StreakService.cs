using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.EntityFrameworkCore;

namespace Lumino.Api.Application.Services
{
    public class StreakService : IStreakService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;

        public StreakService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
        }

        public StreakResponse GetMyStreak(int userId)
        {
            var todayUtc = _dateTimeProvider.UtcNow.Date;

            var streak = _dbContext.UserStreaks.FirstOrDefault(x => x.UserId == userId);

            if (streak == null)
            {
                return new StreakResponse
                {
                    Current = 0,
                    Best = 0,
                    LastActivityDateUtc = DateTime.MinValue
                };
            }

            var lastDate = streak.LastActivityDateUtc.Date;

            if (lastDate < todayUtc.AddDays(-1) && streak.CurrentStreak != 0)
            {
                streak.CurrentStreak = 0;
                _dbContext.SaveChanges();
            }

            return new StreakResponse
            {
                Current = streak.CurrentStreak,
                Best = streak.BestStreak,
                LastActivityDateUtc = streak.LastActivityDateUtc
            };
        }

        public StreakCalendarResponse GetMyCalendar(int userId, int days)
        {
            if (days <= 0)
            {
                days = 30;
            }

            if (days > 365)
            {
                days = 365;
            }

            var nowUtc = _dateTimeProvider.UtcNow.Date;
            var fromDate = nowUtc.AddDays(-(days - 1));

            var activeDates = _dbContext.UserDailyActivities
                .Where(x => x.UserId == userId && x.DateUtc >= fromDate && x.DateUtc <= nowUtc)
                .Select(x => x.DateUtc.Date)
                .ToHashSet();

            var result = new StreakCalendarResponse
            {
                Year = nowUtc.Year,
                Month = nowUtc.Month
            };

            for (var i = days - 1; i >= 0; i--)
            {
                var date = nowUtc.AddDays(-i);

                result.Days.Add(new StreakCalendarDayResponse
                {
                    DateUtc = date,
                    IsActive = activeDates.Contains(date)
                });
            }

            return result;
        }

        public StreakCalendarResponse GetMyCalendarMonth(int userId, int year, int month)
        {
            if (year < 2000 || year > 2100)
            {
                throw new ArgumentException("Invalid year");
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentException("Invalid month");
            }

            var fromDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            var activeDates = _dbContext.UserDailyActivities
                .Where(x => x.UserId == userId && x.DateUtc >= fromDate && x.DateUtc <= toDate)
                .Select(x => x.DateUtc.Date)
                .ToHashSet();

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var result = new StreakCalendarResponse
            {
                Year = year,
                Month = month
            };

            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

                result.Days.Add(new StreakCalendarDayResponse
                {
                    DateUtc = date,
                    IsActive = activeDates.Contains(date)
                });
            }

            return result;
        }

        public void RegisterLessonActivity(int userId)
        {
            var todayUtc = _dateTimeProvider.UtcNow.Date;

            var activeRow = _dbContext.UserDailyActivities.FirstOrDefault(x => x.UserId == userId && x.DateUtc == todayUtc);

            if (activeRow == null)
            {
                _dbContext.UserDailyActivities.Add(new UserDailyActivity
                {
                    UserId = userId,
                    DateUtc = todayUtc
                });
            }

            var streak = _dbContext.UserStreaks.FirstOrDefault(x => x.UserId == userId);

            if (streak == null)
            {
                streak = new UserStreak
                {
                    UserId = userId,
                    CurrentStreak = 1,
                    BestStreak = 1,
                    LastActivityDateUtc = todayUtc
                };

                _dbContext.UserStreaks.Add(streak);
                _dbContext.SaveChanges();
                return;
            }

            var lastDate = streak.LastActivityDateUtc.Date;

            if (lastDate == todayUtc)
            {
                _dbContext.SaveChanges();
                return;
            }

            if (lastDate == todayUtc.AddDays(-1))
            {
                streak.CurrentStreak += 1;
            }
            else
            {
                streak.CurrentStreak = 1;
            }

            if (streak.CurrentStreak > streak.BestStreak)
            {
                streak.BestStreak = streak.CurrentStreak;
            }

            streak.LastActivityDateUtc = todayUtc;

            _dbContext.SaveChanges();
        }
    }
}
