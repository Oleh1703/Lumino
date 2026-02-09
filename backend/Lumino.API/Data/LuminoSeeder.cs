using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
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
            SeedVocabulary(dbContext);
            SeedDemoContentEnglishOnly(dbContext);
        }

        private static void SeedAdmin(LuminoDbContext dbContext)
        {
            var adminEmail = "admin@lumino.local";

            var admin = dbContext.Users.FirstOrDefault(x => x.Email == adminEmail);
            if (admin != null)
            {
                return;
            }

            var hasher = new PasswordHasher();

            admin = new User
            {
                Email = adminEmail,
                PasswordHash = hasher.Hash("Admin123!"),
                Role = Role.Admin,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(admin);
            dbContext.SaveChanges();
        }

        private static void SeedAchievements(LuminoDbContext dbContext)
        {
            var achievements = new List<Achievement>
            {
                new Achievement
                {
                    Title = "First Lesson",
                    Description = "Complete your first lesson"
                },
                new Achievement
                {
                    Title = "5 Lessons Completed",
                    Description = "Complete 5 lessons"
                },
                new Achievement
                {
                    Title = "Perfect Lesson",
                    Description = "Complete a lesson without mistakes"
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

            foreach (var item in achievements)
            {
                var fromDb = dbContext.Achievements.FirstOrDefault(x => x.Title == item.Title);

                if (fromDb == null)
                {
                    dbContext.Achievements.Add(item);
                    continue;
                }

                if (fromDb.Description != item.Description)
                {
                    fromDb.Description = item.Description;
                }
            }

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

        private static void SeedVocabulary(LuminoDbContext dbContext)
        {
            if (dbContext.VocabularyItems.Any())
            {
                return;
            }

            var items = new List<VocabularyItem>
            {
                new VocabularyItem { Word = "hello", Translation = "привіт", Example = "Hello! How are you?" },
                new VocabularyItem { Word = "goodbye", Translation = "до побачення", Example = "Goodbye! See you soon." },
                new VocabularyItem { Word = "please", Translation = "будь ласка", Example = "Please, help me." },
                new VocabularyItem { Word = "thank you", Translation = "дякую", Example = "Thank you for your help." },
                new VocabularyItem { Word = "yes", Translation = "так", Example = "Yes, I agree." },
                new VocabularyItem { Word = "no", Translation = "ні", Example = "No, I don't know." },
                new VocabularyItem { Word = "water", Translation = "вода", Example = "I want water." },
                new VocabularyItem { Word = "coffee", Translation = "кава", Example = "Coffee, please." },
                new VocabularyItem { Word = "tea", Translation = "чай", Example = "Tea is hot." },
                new VocabularyItem { Word = "bread", Translation = "хліб", Example = "I like bread." },

                new VocabularyItem { Word = "airport", Translation = "аеропорт", Example = "The airport is big." },
                new VocabularyItem { Word = "ticket", Translation = "квиток", Example = "I have a ticket." },
                new VocabularyItem { Word = "hotel", Translation = "готель", Example = "The hotel is nice." },
                new VocabularyItem { Word = "room", Translation = "кімната", Example = "This is my room." },
                new VocabularyItem { Word = "train", Translation = "поїзд", Example = "The train is fast." },
                new VocabularyItem { Word = "bus", Translation = "автобус", Example = "The bus is late." },
                new VocabularyItem { Word = "where", Translation = "де", Example = "Where are you?" },
                new VocabularyItem { Word = "how much", Translation = "скільки коштує", Example = "How much is it?" },
                new VocabularyItem { Word = "open", Translation = "відкрито", Example = "The shop is open." },
                new VocabularyItem { Word = "closed", Translation = "закрито", Example = "The shop is closed." }
            };

            dbContext.VocabularyItems.AddRange(items);
            dbContext.SaveChanges();
        }

        private static void SeedDemoContentEnglishOnly(LuminoDbContext dbContext)
        {
            if (dbContext.Courses.Any(x => x.Title == "English A1"))
            {
                return;
            }

            var courseEnglish = new Course
            {
                Title = "English A1",
                Description = "Basics: greetings, numbers, simple phrases",
                IsPublished = true
            };

            dbContext.Courses.Add(courseEnglish);
            dbContext.SaveChanges();

            var topic1 = new Topic
            {
                CourseId = courseEnglish.Id,
                Title = "Greetings",
                Order = 1
            };

            var topic2 = new Topic
            {
                CourseId = courseEnglish.Id,
                Title = "Numbers",
                Order = 2
            };

            dbContext.Topics.AddRange(topic1, topic2);
            dbContext.SaveChanges();

            var lesson1 = new Lesson
            {
                TopicId = topic1.Id,
                Title = "Hello / Goodbye",
                Theory = "Hello = Привіт\nGoodbye = До побачення\nPlease = Будь ласка\nThank you = Дякую",
                Order = 1
            };

            var lesson2 = new Lesson
            {
                TopicId = topic1.Id,
                Title = "How are you?",
                Theory = "How are you? = Як ти?\nI'm fine = У мене все добре\nAnd you? = А ти?",
                Order = 2
            };

            var lesson3 = new Lesson
            {
                TopicId = topic1.Id,
                Title = "Introducing yourself",
                Theory = "My name is ... = Мене звати ...\nNice to meet you = Приємно познайомитись",
                Order = 3
            };

            var lesson4 = new Lesson
            {
                TopicId = topic1.Id,
                Title = "Polite words",
                Theory = "Sorry = Пробач\nExcuse me = Перепрошую\nYou're welcome = Нема за що",
                Order = 4
            };

            var lesson5 = new Lesson
            {
                TopicId = topic2.Id,
                Title = "Numbers 1-5",
                Theory = "One, Two, Three, Four, Five",
                Order = 1
            };

            var lesson6 = new Lesson
            {
                TopicId = topic2.Id,
                Title = "Numbers 6-10",
                Theory = "Six, Seven, Eight, Nine, Ten",
                Order = 2
            };

            var lesson7 = new Lesson
            {
                TopicId = topic2.Id,
                Title = "How much is it?",
                Theory = "How much is it? = Скільки коштує?\nIt is ... = Це коштує ...",
                Order = 3
            };

            var lesson8 = new Lesson
            {
                TopicId = topic2.Id,
                Title = "Time basics",
                Theory = "What time is it? = Котра година?\nIt's ... o'clock = Зараз ... година",
                Order = 4
            };

            dbContext.Lessons.AddRange(lesson1, lesson2, lesson3, lesson4, lesson5, lesson6, lesson7, lesson8);
            dbContext.SaveChanges();

            AddExercises(dbContext, lesson1, new List<ExerciseSeed>
            {
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "Hello = ?", "[\"Привіт\",\"До побачення\",\"Дякую\"]", "Привіт"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write Ukrainian for: Goodbye", "{}", "До побачення"),
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "Please = ?", "[\"Будь ласка\",\"Пробач\",\"Нема за що\"]", "Будь ласка"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write Ukrainian for: Thank you", "{}", "Дякую")
            });

            AddExercises(dbContext, lesson2, new List<ExerciseSeed>
            {
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "How are you? = ?", "[\"Як ти?\",\"Де ти?\",\"Хто ти?\"]", "Як ти?"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: У мене все добре", "{}", "I'm fine"),
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "And you? = ?", "[\"А ти?\",\"І я\",\"Ти добре?\"]", "А ти?"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Як ти?", "{}", "How are you?")
            });

            AddExercises(dbContext, lesson3, new List<ExerciseSeed>
            {
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "My name is ... = ?", "[\"Мене звати ...\",\"Я добре\",\"Я тут\"]", "Мене звати ..."),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Мене звати Анна", "{}", "My name is Anna"),
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "Nice to meet you = ?", "[\"Приємно познайомитись\",\"Добрий ранок\",\"До побачення\"]", "Приємно познайомитись"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Приємно познайомитись", "{}", "Nice to meet you")
            });

            AddExercises(dbContext, lesson4, new List<ExerciseSeed>
            {
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "Sorry = ?", "[\"Пробач\",\"Будь ласка\",\"Дякую\"]", "Пробач"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Перепрошую", "{}", "Excuse me"),
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "You're welcome = ?", "[\"Нема за що\",\"До побачення\",\"Привіт\"]", "Нема за що"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write Ukrainian for: Excuse me", "{}", "Перепрошую")
            });

            AddExercises(dbContext, lesson5, new List<ExerciseSeed>
            {
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "Three = ?", "[\"Три\",\"Чотири\",\"П'ять\"]", "Три"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Два", "{}", "Two"),
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "One = ?", "[\"Один\",\"Нуль\",\"П'ять\"]", "Один"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: П'ять", "{}", "Five")
            });

            AddExercises(dbContext, lesson6, new List<ExerciseSeed>
            {
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "Seven = ?", "[\"Сім\",\"Шість\",\"Вісім\"]", "Сім"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Десять", "{}", "Ten"),
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "Nine = ?", "[\"Дев'ять\",\"Вісім\",\"Сім\"]", "Дев'ять"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Шість", "{}", "Six")
            });

            AddExercises(dbContext, lesson7, new List<ExerciseSeed>
            {
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "How much is it? = ?", "[\"Скільки коштує?\",\"Де ти?\",\"Котра година?\"]", "Скільки коштує?"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Це коштує 5", "{}", "It is 5"),
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "It is ... = ?", "[\"Це коштує ...\",\"Мене звати ...\",\"Я добре\"]", "Це коштує ..."),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Скільки коштує?", "{}", "How much is it?")
            });

            AddExercises(dbContext, lesson8, new List<ExerciseSeed>
            {
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "What time is it? = ?", "[\"Котра година?\",\"Скільки коштує?\",\"Як тебе звати?\"]", "Котра година?"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Зараз 7 година", "{}", "It's 7 o'clock"),
                new ExerciseSeed(Domain.Enums.ExerciseType.MultipleChoice, "It's ... o'clock = ?", "[\"Зараз ... година\",\"Це коштує ...\",\"Мене звати ...\"]", "Зараз ... година"),
                new ExerciseSeed(Domain.Enums.ExerciseType.Input, "Write English: Котра година?", "{}", "What time is it?")
            });
        }

        private static void AddExercises(
            LuminoDbContext dbContext,
            Lesson lesson,
            List<ExerciseSeed> seeds)
        {
            var order = 1;

            var exercises = seeds
                .Select(x => new Exercise
                {
                    LessonId = lesson.Id,
                    Type = x.Type,
                    Question = x.Question,
                    Data = x.Data,
                    CorrectAnswer = x.CorrectAnswer,
                    Order = order++
                })
                .ToList();

            dbContext.Exercises.AddRange(exercises);
            dbContext.SaveChanges();
        }

        private record ExerciseSeed(
            Domain.Enums.ExerciseType Type,
            string Question,
            string Data,
            string CorrectAnswer);
    }
}
