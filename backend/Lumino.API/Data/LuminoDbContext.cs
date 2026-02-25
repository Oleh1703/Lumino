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
        public DbSet<LessonVocabulary> LessonVocabularies => Set<LessonVocabulary>();
        public DbSet<ExerciseVocabulary> ExerciseVocabularies => Set<ExerciseVocabulary>();
        public DbSet<Achievement> Achievements => Set<Achievement>();
        public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
        public DbSet<Scene> Scenes => Set<Scene>();
        public DbSet<SceneStep> SceneSteps => Set<SceneStep>();
        public DbSet<SceneAttempt> SceneAttempts => Set<SceneAttempt>();
        public DbSet<UserCourse> UserCourses => Set<UserCourse>();
        public DbSet<UserLessonProgress> UserLessonProgresses => Set<UserLessonProgress>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
        public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(x => x.Email).IsUnique();

                entity.HasIndex(x => x.Username)
                    .IsUnique()
                    .HasFilter("[Username] IS NOT NULL");

                entity.Property(x => x.Email).IsRequired();
                entity.Property(x => x.PasswordHash).IsRequired();

                entity.Property(x => x.NativeLanguageCode).HasMaxLength(10);
                entity.Property(x => x.TargetLanguageCode).HasMaxLength(10);

                entity.Property(x => x.Username).HasMaxLength(32);
                entity.Property(x => x.AvatarUrl).HasMaxLength(256);
                entity.Property(x => x.Theme).HasMaxLength(20).HasDefaultValue("light");

                entity.Property(x => x.IsEmailVerified).HasDefaultValue(false);
            });

            modelBuilder.Entity<EmailVerificationToken>(entity =>
            {
                entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
                entity.Property(x => x.Ip).HasMaxLength(100);
                entity.Property(x => x.UserAgent).HasMaxLength(300);

                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => new { x.UserId, x.ExpiresAt });

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
                entity.Property(x => x.Ip).HasMaxLength(100);
                entity.Property(x => x.UserAgent).HasMaxLength(300);

                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => new { x.UserId, x.ExpiresAt });

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<UserExternalLogin>(entity =>
            {
                entity.Property(x => x.Provider).IsRequired().HasMaxLength(20);
                entity.Property(x => x.ProviderUserId).IsRequired().HasMaxLength(200);
                entity.Property(x => x.Email).HasMaxLength(256);

                entity.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
                entity.HasIndex(x => x.UserId);

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Course>(entity =>
            {
                entity.Property(x => x.Title).IsRequired();
                entity.Property(x => x.Description).IsRequired();

                entity.Property(x => x.LanguageCode).IsRequired().HasMaxLength(10).HasDefaultValue("en");
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
                entity.Property(x => x.IdempotencyKey)
                    .HasMaxLength(64);

                entity.Property(x => x.MistakesIdempotencyKey)
                    .HasMaxLength(64);
                // idempotency key must be unique per user (when provided)
                entity.HasIndex(x => new { x.UserId, x.IdempotencyKey })
                    .IsUnique()
                    .HasFilter("[IdempotencyKey] IS NOT NULL");

                entity.HasIndex(x => new { x.UserId, x.MistakesIdempotencyKey })
                    .IsUnique()
                    .HasFilter("[MistakesIdempotencyKey] IS NOT NULL");
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

                entity.Property(x => x.ReviewIdempotencyKey)
                    .HasMaxLength(64);

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

            modelBuilder.Entity<LessonVocabulary>(entity =>
            {
                entity.HasOne<Lesson>()
                    .WithMany()
                    .HasForeignKey(x => x.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<VocabularyItem>()
                    .WithMany()
                    .HasForeignKey(x => x.VocabularyItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.LessonId, x.VocabularyItemId }).IsUnique();
                entity.HasIndex(x => x.LessonId);
                entity.HasIndex(x => x.VocabularyItemId);
            });

            modelBuilder.Entity<ExerciseVocabulary>(entity =>
            {
                entity.HasOne<Exercise>()
                    .WithMany()
                    .HasForeignKey(x => x.ExerciseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<VocabularyItem>()
                    .WithMany()
                    .HasForeignKey(x => x.VocabularyItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.ExerciseId, x.VocabularyItemId }).IsUnique();
                entity.HasIndex(x => x.ExerciseId);
                entity.HasIndex(x => x.VocabularyItemId);
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

                entity.HasOne<Course>()
                    .WithMany()
                    .HasForeignKey(x => x.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => x.CourseId);

                // NEW: stable scene ordering (Order > 0), fallback to Id when Order == 0 in services.
                entity.HasIndex(x => new { x.CourseId, x.Order }).IsUnique().HasFilter("[Order] > 0");
            });

            modelBuilder.Entity<SceneStep>(entity =>
            {
                entity.Property(x => x.Speaker).IsRequired();
                entity.Property(x => x.Text).IsRequired();
                entity.Property(x => x.StepType).IsRequired();

                entity.HasOne<Scene>()
                    .WithMany()
                    .HasForeignKey(x => x.SceneId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.SceneId, x.Order }).IsUnique();
            });

            modelBuilder.Entity<SceneAttempt>(entity =>
            {
                entity.Property(x => x.IdempotencyKey)
                    .HasMaxLength(64);

                entity.Property(x => x.SubmitIdempotencyKey)
                    .HasMaxLength(64);

                entity.Property(x => x.MistakesIdempotencyKey)
                    .HasMaxLength(64);

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

            modelBuilder.Entity<UserCourse>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Course>()
                    .WithMany()
                    .HasForeignKey(x => x.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
                entity.HasIndex(x => new { x.UserId, x.IsActive });
            });

            modelBuilder.Entity<UserLessonProgress>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Lesson>()
                    .WithMany()
                    .HasForeignKey(x => x.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();
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
