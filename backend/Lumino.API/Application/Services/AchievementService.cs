using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;

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
            GrantStreakStarter(userId);
        }

        public void CheckAndGrantSceneAchievements(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("UserId is invalid");
            }

            GrantFirstScene(userId);
            GrantFiveScenes(userId);
            GrantStreakStarter(userId);
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
            int bestTotalScore = _dbContext.LessonResults
                .Where(x => x.UserId == userId)
                .GroupBy(x => x.LessonId)
                .Select(g => g.Max(x => x.Score))
                .Sum();

            if (bestTotalScore < 100) return;

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

        private void GrantStreakStarter(int userId)
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

            int maxStreak = CalculateMaxStreak(dates);

            if (maxStreak < 3) return;

            var achievement = GetOrCreateAchievement(
                "Streak Starter",
                "Study 3 days in a row"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
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
