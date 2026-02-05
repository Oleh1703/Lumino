using Lumino.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lumino.Api.Data
{
    public class LuminoDbContext : DbContext
    {
        public LuminoDbContext(DbContextOptions<LuminoDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Topic> Topics => Set<Topic>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<Exercise> Exercises => Set<Exercise>();
        public DbSet<LessonResult> LessonResults => Set<LessonResult>();
        public DbSet<UserProgress> UserProgresses => Set<UserProgress>();
        public DbSet<VocabularyItem> VocabularyItems => Set<VocabularyItem>();
        public DbSet<UserVocabulary> UserVocabularies => Set<UserVocabulary>();
        public DbSet<Achievement> Achievements => Set<Achievement>();
        public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
        public DbSet<Scene> Scenes => Set<Scene>();
        public DbSet<SceneAttempt> SceneAttempts => Set<SceneAttempt>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    }
}
