using System.Security.Cryptography;
using System.Text;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lumino.Api.Data
{
    public static class LuminoSeeder
    {
        public static void Seed(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.Migrate();

            SeedAdmin(dbContext);
            SeedAchievements(dbContext);
            SeedScenes(dbContext);
            SeedDemoContent(dbContext);
        }

        private static void SeedAdmin(LuminoDbContext dbContext)
        {
            var adminEmail = "admin@lumino.local";

            var admin = dbContext.Users.FirstOrDefault(x => x.Email == adminEmail);
            if (admin != null)
            {
                return;
            }

            admin = new User
            {
                Email = adminEmail,
                PasswordHash = HashPassword("Admin123!"),
                Role = Role.Admin,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(admin);
            dbContext.SaveChanges();
        }

        private static void SeedAchievements(LuminoDbContext dbContext)
        {
            if (dbContext.Achievements.Any())
            {
                return;
            }

            var achievements = new List<Achievement>
            {
                new Achievement
                {
                    Title = "First Lesson",
                    Description = "Complete your first lesson"
                },
                new Achievement
                {
                    Title = "100 XP",
                    Description = "Earn 100 total score"
                },
                new Achievement
                {
                    Title = "Streak Starter",
                    Description = "Study 3 days in a row"
                }
            };

            dbContext.Achievements.AddRange(achievements);
            dbContext.SaveChanges();
        }

        private static void SeedScenes(LuminoDbContext dbContext)
        {
            if (dbContext.Scenes.Any())
            {
                return;
            }

            var scenes = new List<Scene>
            {
                new Scene
                {
                    Title = "Cafe order",
                    Description = "Order a coffee in a cafe",
                    SceneType = "Dialog"
                },
                new Scene
                {
                    Title = "Airport check-in",
                    Description = "Check in for your flight",
                    SceneType = "Dialog"
                }
            };

            dbContext.Scenes.AddRange(scenes);
            dbContext.SaveChanges();
        }

        private static void SeedDemoContent(LuminoDbContext dbContext)
        {
            if (dbContext.Courses.Any())
            {
                return;
            }

            var course = new Course
            {
                Title = "English A1",
                Description = "Basics: greetings, numbers, simple phrases",
                IsPublished = true
            };

            dbContext.Courses.Add(course);
            dbContext.SaveChanges();

            var topic1 = new Topic
            {
                CourseId = course.Id,
                Title = "Greetings",
                Order = 1
            };

            var topic2 = new Topic
            {
                CourseId = course.Id,
                Title = "Numbers",
                Order = 2
            };

            dbContext.Topics.AddRange(topic1, topic2);
            dbContext.SaveChanges();

            var lesson1 = new Lesson
            {
                TopicId = topic1.Id,
                Title = "Hello / Goodbye",
                Theory = "Hello = Привіт\nGoodbye = До побачення",
                Order = 1
            };

            var lesson2 = new Lesson
            {
                TopicId = topic1.Id,
                Title = "How are you?",
                Theory = "How are you? = Як ти?\nI'm fine = У мене все добре",
                Order = 2
            };

            var lesson3 = new Lesson
            {
                TopicId = topic2.Id,
                Title = "1-5",
                Theory = "One, Two, Three, Four, Five",
                Order = 1
            };

            dbContext.Lessons.AddRange(lesson1, lesson2, lesson3);
            dbContext.SaveChanges();

            var exercises = new List<Exercise>
            {
                new Exercise
                {
                    LessonId = lesson1.Id,
                    Type = ExerciseType.MultipleChoice,
                    Question = "Hello = ?",
                    Data = "[\"Привіт\",\"До побачення\",\"Дякую\"]",
                    CorrectAnswer = "Привіт",
                    Order = 1
                },
                new Exercise
                {
                    LessonId = lesson1.Id,
                    Type = ExerciseType.Input,
                    Question = "Goodbye = (write Ukrainian)",
                    Data = "{}",
                    CorrectAnswer = "До побачення",
                    Order = 2
                },
                new Exercise
                {
                    LessonId = lesson2.Id,
                    Type = ExerciseType.Match,
                    Question = "Match phrases",
                    Data = "{\"pairs\":[{\"left\":\"How are you?\",\"right\":\"Як ти?\"},{\"left\":\"I'm fine\",\"right\":\"У мене все добре\"}]}",
                    CorrectAnswer = "pairs",
                    Order = 1
                },
                new Exercise
                {
                    LessonId = lesson3.Id,
                    Type = ExerciseType.MultipleChoice,
                    Question = "Three = ?",
                    Data = "[\"Три\",\"Чотири\",\"П'ять\"]",
                    CorrectAnswer = "Три",
                    Order = 1
                }
            };

            dbContext.Exercises.AddRange(exercises);
            dbContext.SaveChanges();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
