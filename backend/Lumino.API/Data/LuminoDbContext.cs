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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(x => x.Email).IsUnique();

                entity.Property(x => x.Email).IsRequired();
                entity.Property(x => x.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.Property(x => x.Title).IsRequired();
                entity.Property(x => x.Description).IsRequired();
            });

            modelBuilder.Entity<Topic>(entity =>
            {
                entity.Property(x => x.Title).IsRequired();

                entity.HasOne<Course>()
                    .WithMany()
                    .HasForeignKey(x => x.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.CourseId, x.Order });
            });

            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.Property(x => x.Title).IsRequired();
                entity.Property(x => x.Theory).IsRequired();

                entity.HasOne<Topic>()
                    .WithMany()
                    .HasForeignKey(x => x.TopicId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.TopicId, x.Order });
            });

            modelBuilder.Entity<Exercise>(entity =>
            {
                entity.Property(x => x.Question).IsRequired();
                entity.Property(x => x.Data).IsRequired();
                entity.Property(x => x.CorrectAnswer).IsRequired();

                entity.HasOne<Lesson>()
                    .WithMany()
                    .HasForeignKey(x => x.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.LessonId);
            });

            modelBuilder.Entity<LessonResult>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Lesson>()
                    .WithMany()
                    .HasForeignKey(x => x.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.LessonId });
            });

            modelBuilder.Entity<UserProgress>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.UserId).IsUnique();
            });

            modelBuilder.Entity<UserVocabulary>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<VocabularyItem>()
                    .WithMany()
                    .HasForeignKey(x => x.VocabularyItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.VocabularyItemId }).IsUnique();
            });

            modelBuilder.Entity<UserAchievement>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Achievement>()
                    .WithMany()
                    .HasForeignKey(x => x.AchievementId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.AchievementId }).IsUnique();
            });

            modelBuilder.Entity<Scene>(entity =>
            {
                entity.Property(x => x.Title).IsRequired();
                entity.Property(x => x.Description).IsRequired();
                entity.Property(x => x.SceneType).IsRequired();
            });

            modelBuilder.Entity<SceneAttempt>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Scene>()
                    .WithMany()
                    .HasForeignKey(x => x.SceneId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.SceneId }).IsUnique();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.Property(x => x.TokenHash).IsRequired();
                entity.HasIndex(x => x.TokenHash).IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.RefreshTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.UserId);
            });
        }
    }
}
