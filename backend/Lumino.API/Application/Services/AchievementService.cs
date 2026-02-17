using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumino.Api.Application.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly LearningSettings _learningSettings;

        public AchievementService(
            LuminoDbContext dbContext,
            IDateTimeProvider dateTimeProvider,
            IOptions<LearningSettings> learningSettings)
        {
            _dbContext = dbContext;
            _dateTimeProvider = dateTimeProvider;
            _learningSettings = learningSettings.Value;
        }

        public void CheckAndGrantAchievements(int userId, int lessonScore, int totalQuestions)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("UserId is invalid");
            }

            if (lessonScore < 0 || totalQuestions < 0)
            {
                throw new ArgumentException("Lesson result values are invalid");
            }

            GrantFirstLesson(userId);
            GrantFiveLessons(userId);
            GrantPerfectLesson(userId, lessonScore, totalQuestions);
            GrantHundredXp(userId);

            // Streak achievements (study days)
            GrantStreakStarter(userId);
            GrantStreak7(userId);
            GrantStreak30(userId);

            // Daily goal
            GrantDailyGoal(userId);
        }

        public void CheckAndGrantSceneAchievements(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("UserId is invalid");
            }

            GrantFirstScene(userId);
            GrantFiveScenes(userId);

            // Streak achievements (study days)
            GrantStreakStarter(userId);
            GrantStreak7(userId);
            GrantStreak30(userId);

            // Daily goal
            GrantDailyGoal(userId);
        }

        private void GrantFirstLesson(int userId)
        {
            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            int passedDistinctLessons = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .Select(x => x.LessonId)
                .Distinct()
                .Count();

            if (passedDistinctLessons < 1) return;

            var achievement = GetOrCreateAchievement(
                "First Lesson",
                "Complete your first lesson"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantFiveLessons(int userId)
        {
            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            int passedDistinctLessons = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .Select(x => x.LessonId)
                .Distinct()
                .Count();

            if (passedDistinctLessons < 5) return;

            var achievement = GetOrCreateAchievement(
                "5 Lessons Completed",
                "Complete 5 lessons"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantPerfectLesson(int userId, int score, int total)
        {
            if (total <= 0 || score != total) return;

            var achievement = GetOrCreateAchievement(
                "Perfect Lesson",
                "Complete a lesson without mistakes"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantHundredXp(int userId)
        {
            // "100 XP" має відповідати реальному TotalScore (уроки + сцени)
            int lessonsScore = _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .GroupBy(x => x.LessonId)
                .Select(g => g.Max(x => x.Score))
                .Sum();

            int completedDistinctScenes = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .Distinct()
                .Count();

            int scenesScore = completedDistinctScenes * _learningSettings.SceneCompletionScore;

            int totalScore = lessonsScore + scenesScore;

            if (totalScore < 100) return;

            var achievement = GetOrCreateAchievement(
                "100 XP",
                "Earn 100 total score"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantFirstScene(int userId)
        {
            int completedDistinctScenes = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .Distinct()
                .Count();

            if (completedDistinctScenes < 1) return;

            var achievement = GetOrCreateAchievement(
                "First Scene",
                "Complete your first scene"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantFiveScenes(int userId)
        {
            int completedDistinctScenes = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.SceneId)
                .Distinct()
                .Count();

            if (completedDistinctScenes < 5) return;

            var achievement = GetOrCreateAchievement(
                "5 Scenes Completed",
                "Complete 5 scenes"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantDailyGoal(int userId)
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

            if (todayScore < targetScore) return;

            var achievement = GetOrCreateAchievement(
                "Daily Goal",
                "Reach your daily goal"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantStreakStarter(int userId)
        {
            int maxStreak = CalculateUserMaxStreak(userId);

            if (maxStreak < 3) return;

            var achievement = GetOrCreateAchievement(
                "Streak Starter",
                "Study 3 days in a row"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantStreak7(int userId)
        {
            int maxStreak = CalculateUserMaxStreak(userId);

            if (maxStreak < 7) return;

            var achievement = GetOrCreateAchievement(
                "Streak 7",
                "Study 7 days in a row"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantStreak30(int userId)
        {
            int maxStreak = CalculateUserMaxStreak(userId);

            if (maxStreak < 30) return;

            var achievement = GetOrCreateAchievement(
                "Streak 30",
                "Study 30 days in a row"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private int CalculateUserMaxStreak(int userId)
        {
            int passingScorePercent = LessonPassingRules.NormalizePassingPercent(_learningSettings.PassingScorePercent);

            var passedLessonDates = _dbContext.LessonResults
                .Where(x =>
                    x.UserId == userId &&
                    x.TotalQuestions > 0 &&
                    x.Score * 100 >= x.TotalQuestions * passingScorePercent
                )
                .Select(x => x.CompletedAt.Date)
                .ToList();

            var sceneDates = _dbContext.SceneAttempts
                .Where(x => x.UserId == userId && x.IsCompleted)
                .Select(x => x.CompletedAt.Date)
                .ToList();

            var dates = passedLessonDates
                .Concat(sceneDates)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return CalculateMaxStreak(dates);
        }

        private static int CalculateMaxStreak(List<DateTime> datesSortedAsc)
        {
            if (datesSortedAsc == null || datesSortedAsc.Count == 0)
            {
                return 0;
            }

            int max = 1;
            int current = 1;

            for (int i = 1; i < datesSortedAsc.Count; i++)
            {
                if (datesSortedAsc[i] == datesSortedAsc[i - 1].AddDays(1))
                {
                    current++;
                    if (current > max) max = current;
                    continue;
                }

                current = 1;
            }

            return max;
        }

        private Achievement GetOrCreateAchievement(string title, string description)
        {
            var achievement = _dbContext.Achievements.FirstOrDefault(x => x.Title == title);

            if (achievement != null)
            {
                if (achievement.Description != description)
                {
                    achievement.Description = description;
                    _dbContext.SaveChanges();
                }

                return achievement;
            }

            achievement = new Achievement
            {
                Title = title,
                Description = description
            };

            _dbContext.Achievements.Add(achievement);
            _dbContext.SaveChanges();

            return achievement;
        }

        private void GrantToUserIfNotExists(int userId, int achievementId)
        {
            bool alreadyHas = _dbContext.UserAchievements
                .Any(x => x.UserId == userId && x.AchievementId == achievementId);

            if (alreadyHas) return;

            _dbContext.UserAchievements.Add(new UserAchievement
            {
                UserId = userId,
                AchievementId = achievementId,
                EarnedAt = _dateTimeProvider.UtcNow
            });

            _dbContext.SaveChanges();
        }
    }
}
