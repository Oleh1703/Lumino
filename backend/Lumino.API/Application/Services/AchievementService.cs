using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;

namespace Lumino.Api.Application.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly LuminoDbContext _dbContext;

        public AchievementService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
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
        }

        private void GrantFirstLesson(int userId)
        {
            int lessonsCount = _dbContext.LessonResults.Count(x => x.UserId == userId);
            if (lessonsCount < 1) return;

            var achievement = GetOrCreateAchievement(
                "First Lesson",
                "You completed your first lesson!"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantFiveLessons(int userId)
        {
            int lessonsCount = _dbContext.LessonResults.Count(x => x.UserId == userId);
            if (lessonsCount < 5) return;

            var achievement = GetOrCreateAchievement(
                "5 Lessons Completed",
                "You completed 5 lessons!"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private void GrantPerfectLesson(int userId, int score, int total)
        {
            if (total <= 0 || score != total) return;

            var achievement = GetOrCreateAchievement(
                "Perfect Lesson",
                "You completed a lesson without mistakes!"
            );

            GrantToUserIfNotExists(userId, achievement.Id);
        }

        private Achievement GetOrCreateAchievement(string title, string description)
        {
            var achievement = _dbContext.Achievements.FirstOrDefault(x => x.Title == title);

            if (achievement != null)
            {
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
                EarnedAt = DateTime.UtcNow
            });

            _dbContext.SaveChanges();
        }
    }
}
