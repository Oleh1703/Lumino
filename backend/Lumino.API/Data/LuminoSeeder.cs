using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
            SeedUser(dbContext);

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

        private static void SeedUser(LuminoDbContext dbContext)
        {
            var userEmail = "user@lumino.local";

            var user = dbContext.Users.FirstOrDefault(x => x.Email == userEmail);
            if (user != null)
            {
                return;
            }

            var hasher = new PasswordHasher();

            user = new User
            {
                Email = userEmail,
                PasswordHash = hasher.Hash("User123!"),
                Role = Role.User,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
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
                    Title = "First Scene",
                    Description = "Complete your first scene"
                },
                new Achievement
                {
                    Title = "5 Scenes Completed",
                    Description = "Complete 5 scenes"
                },
                new Achievement
                {
                    Title = "Streak Starter",
                    Description = "Study 3 days in a row"
                }
            };

            var fromDbList = dbContext.Achievements.ToList();
            var fromDbMap = fromDbList
                .GroupBy(x => x.Title)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in achievements)
            {
                if (!fromDbMap.TryGetValue(item.Title, out var fromDb))
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
            var scenes = new List<Scene>
            {
                new Scene
                {
                    Title = "Cafe order",
                    Description = "Order a coffee in a cafe",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null
                },
                new Scene
                {
                    Title = "Airport check-in",
                    Description = "Check in for your flight",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null
                },
                new Scene
                {
                    Title = "Hotel booking",
                    Description = "Book a room at a hotel reception",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null
                },
                new Scene
                {
                    Title = "Asking directions",
                    Description = "Ask how to get to a place in the city",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null
                },
                new Scene
                {
                    Title = "Shopping",
                    Description = "Buy something in a store and ask the price",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null
                },
                new Scene
                {
                    Title = "Small talk",
                    Description = "Introduce yourself and keep a short conversation",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null
                }
            };

            var fromDbList = dbContext.Scenes.ToList();
            var fromDbMap = fromDbList
                .GroupBy(x => x.Title)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in scenes)
            {
                if (!fromDbMap.TryGetValue(item.Title, out var fromDb))
                {
                    dbContext.Scenes.Add(item);
                    continue;
                }

                if (fromDb.Description != item.Description)
                {
                    fromDb.Description = item.Description;
                }

                if (fromDb.SceneType != item.SceneType)
                {
                    fromDb.SceneType = item.SceneType;
                }

                // медіа не перетираємо null-ами (щоб адмінські зміни не зникали)
                if (!string.IsNullOrWhiteSpace(item.BackgroundUrl) && fromDb.BackgroundUrl != item.BackgroundUrl)
                {
                    fromDb.BackgroundUrl = item.BackgroundUrl;
                }

                if (!string.IsNullOrWhiteSpace(item.AudioUrl) && fromDb.AudioUrl != item.AudioUrl)
                {
                    fromDb.AudioUrl = item.AudioUrl;
                }
            }

            dbContext.SaveChanges();

            var sceneMap = dbContext.Scenes
                .ToList()
                .GroupBy(x => x.Title)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            var stepsBySceneTitle = new Dictionary<string, List<SceneStepSeed>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Cafe order"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "Barista", "Hello! What would you like?", "Line"),
                    new SceneStepSeed(2, "You", "Hi! I'd like a coffee, please.", "Line"),
                    new SceneStepSeed(3, "Barista", "Sure. Small or large?", "Line"),
                    new SceneStepSeed(4, "You", "Small, please.", "Line"),
                    new SceneStepSeed(5, "Barista", "Anything else?", "Line"),
                    new SceneStepSeed(6, "You", "No, thank you.", "Line"),
                    new SceneStepSeed(7, "Barista", "Great. That will be $3.", "Line")
                },
                ["Airport check-in"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "Agent", "Good morning. Can I see your passport?", "Line"),
                    new SceneStepSeed(2, "You", "Yes, here it is.", "Line"),
                    new SceneStepSeed(3, "Agent", "Do you have any bags to check in?", "Line"),
                    new SceneStepSeed(4, "You", "Yes, one bag.", "Line"),
                    new SceneStepSeed(5, "Agent", "Thank you. Your gate is A12.", "Line"),
                    new SceneStepSeed(6, "You", "Thanks!", "Line")
                },
                ["Hotel booking"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "Reception", "Hello! How can I help you?", "Line"),
                    new SceneStepSeed(2, "You", "Hi! I'd like to book a room.", "Line"),
                    new SceneStepSeed(3, "Reception", "How many nights?", "Line"),
                    new SceneStepSeed(4, "You", "Two nights, please.", "Line"),
                    new SceneStepSeed(5, "Reception", "Great. May I have your name?", "Line"),
                    new SceneStepSeed(6, "You", "My name is Alex.", "Line")
                },
                ["Asking directions"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "You", "Excuse me, where is the station?", "Line"),
                    new SceneStepSeed(2, "Person", "Go straight and turn left.", "Line"),
                    new SceneStepSeed(3, "You", "Is it far from here?", "Line"),
                    new SceneStepSeed(4, "Person", "No, it's about 5 minutes.", "Line"),
                    new SceneStepSeed(5, "You", "Thank you!", "Line")
                },
                ["Shopping"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "You", "Hello! How much is this?", "Line"),
                    new SceneStepSeed(2, "Cashier", "It is $10.", "Line"),
                    new SceneStepSeed(3, "You", "Can I pay by card?", "Line"),
                    new SceneStepSeed(4, "Cashier", "Yes, of course.", "Line"),
                    new SceneStepSeed(5, "You", "Thank you.", "Line")
                },
                ["Small talk"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "You", "Hi! My name is Alex.", "Line"),
                    new SceneStepSeed(2, "Person", "Nice to meet you, Alex! I'm Kate.", "Line"),
                    new SceneStepSeed(3, "You", "How are you today?", "Line"),
                    new SceneStepSeed(4, "Person", "I'm fine, thanks. And you?", "Line"),
                    new SceneStepSeed(5, "You", "I'm fine too.", "Line")
                }
            };

            foreach (var kv in stepsBySceneTitle)
            {
                if (!sceneMap.TryGetValue(kv.Key, out var scene))
                {
                    continue;
                }

                var existing = dbContext.SceneSteps
                    .Where(x => x.SceneId == scene.Id)
                    .ToList()
                    .GroupBy(x => x.Order)
                    .ToDictionary(x => x.Key, x => x.First());

                foreach (var seed in kv.Value)
                {
                    if (!existing.TryGetValue(seed.Order, out var step))
                    {
                        dbContext.SceneSteps.Add(new SceneStep
                        {
                            SceneId = scene.Id,
                            Order = seed.Order,
                            Speaker = seed.Speaker,
                            Text = seed.Text,
                            StepType = seed.StepType,
                            MediaUrl = seed.MediaUrl,
                            ChoicesJson = seed.ChoicesJson
                        });

                        continue;
                    }

                    if (step.Speaker != seed.Speaker)
                    {
                        step.Speaker = seed.Speaker;
                    }

                    if (step.Text != seed.Text)
                    {
                        step.Text = seed.Text;
                    }

                    if (step.StepType != seed.StepType)
                    {
                        step.StepType = seed.StepType;
                    }

                    if (step.MediaUrl != seed.MediaUrl)
                    {
                        step.MediaUrl = seed.MediaUrl;
                    }

                    if (step.ChoicesJson != seed.ChoicesJson)
                    {
                        step.ChoicesJson = seed.ChoicesJson;
                    }
                }
            }

            dbContext.SaveChanges();
        }

        private static void SeedVocabulary(LuminoDbContext dbContext)
        {
            var items = new List<VocabularyItem>
            {
                new VocabularyItem { Word = "hello", Translation = "привіт", Example = "Hello! How are you?" },
                new VocabularyItem { Word = "goodbye", Translation = "до побачення", Example = "Goodbye! See you soon." },
                new VocabularyItem { Word = "please", Translation = "будь ласка", Example = "Please, help me." },
                new VocabularyItem { Word = "thank you", Translation = "дякую", Example = "Thank you for your help." },
                new VocabularyItem { Word = "yes", Translation = "так", Example = "Yes, I agree." },
                new VocabularyItem { Word = "no", Translation = "ні", Example = "No, I don't know." },
                new VocabularyItem { Word = "sorry", Translation = "пробач", Example = "Sorry, I'm late." },
                new VocabularyItem { Word = "excuse me", Translation = "перепрошую", Example = "Excuse me, where is the station?" },
                new VocabularyItem { Word = "welcome", Translation = "ласкаво просимо", Example = "Welcome to our city!" },
                new VocabularyItem { Word = "good morning", Translation = "добрий ранок", Example = "Good morning! Have a nice day." },
                new VocabularyItem { Word = "good evening", Translation = "добрий вечір", Example = "Good evening! Nice to see you." },

                new VocabularyItem { Word = "water", Translation = "вода", Example = "I want water." },
                new VocabularyItem { Word = "coffee", Translation = "кава", Example = "Coffee, please." },
                new VocabularyItem { Word = "tea", Translation = "чай", Example = "Tea is hot." },
                new VocabularyItem { Word = "bread", Translation = "хліб", Example = "I like bread." },
                new VocabularyItem { Word = "milk", Translation = "молоко", Example = "Milk in my coffee, please." },
                new VocabularyItem { Word = "sugar", Translation = "цукор", Example = "No sugar, please." },
                new VocabularyItem { Word = "salt", Translation = "сіль", Example = "Add some salt." },
                new VocabularyItem { Word = "menu", Translation = "меню", Example = "Can I see the menu?" },
                new VocabularyItem { Word = "bill", Translation = "рахунок", Example = "Can I have the bill, please?" },

                new VocabularyItem { Word = "airport", Translation = "аеропорт", Example = "The airport is big." },
                new VocabularyItem { Word = "ticket", Translation = "квиток", Example = "I have a ticket." },
                new VocabularyItem { Word = "passport", Translation = "паспорт", Example = "Show me your passport." },
                new VocabularyItem { Word = "plane", Translation = "літак", Example = "The plane is on time." },
                new VocabularyItem { Word = "train", Translation = "поїзд", Example = "The train is fast." },
                new VocabularyItem { Word = "bus", Translation = "автобус", Example = "The bus is late." },
                new VocabularyItem { Word = "station", Translation = "станція", Example = "Where is the station?" },
                new VocabularyItem { Word = "hotel", Translation = "готель", Example = "The hotel is nice." },
                new VocabularyItem { Word = "room", Translation = "кімната", Example = "This is my room." },
                new VocabularyItem { Word = "key", Translation = "ключ", Example = "Here is your key." },

                new VocabularyItem { Word = "where", Translation = "де", Example = "Where are you?" },
                new VocabularyItem { Word = "when", Translation = "коли", Example = "When do we leave?" },
                new VocabularyItem { Word = "who", Translation = "хто", Example = "Who is that?" },
                new VocabularyItem { Word = "what", Translation = "що", Example = "What is this?" },
                new VocabularyItem { Word = "how", Translation = "як", Example = "How are you?" },
                new VocabularyItem { Word = "why", Translation = "чому", Example = "Why are you sad?" },

                new VocabularyItem { Word = "open", Translation = "відкрито", Example = "The shop is open." },
                new VocabularyItem { Word = "closed", Translation = "закрито", Example = "The shop is closed." },
                new VocabularyItem { Word = "left", Translation = "ліворуч", Example = "Turn left." },
                new VocabularyItem { Word = "right", Translation = "праворуч", Example = "Turn right." },
                new VocabularyItem { Word = "straight", Translation = "прямо", Example = "Go straight." },

                new VocabularyItem { Word = "how much", Translation = "скільки коштує", Example = "How much is it?" },
                new VocabularyItem { Word = "price", Translation = "ціна", Example = "The price is high." },
                new VocabularyItem { Word = "cheap", Translation = "дешевий", Example = "This is cheap." },
                new VocabularyItem { Word = "expensive", Translation = "дорогий", Example = "This is expensive." },

                new VocabularyItem { Word = "time", Translation = "час", Example = "What time is it?" },
                new VocabularyItem { Word = "today", Translation = "сьогодні", Example = "Today is Monday." },
                new VocabularyItem { Word = "tomorrow", Translation = "завтра", Example = "See you tomorrow." },
                new VocabularyItem { Word = "yesterday", Translation = "вчора", Example = "Yesterday was cold." }
            };

            var fromDbList = dbContext.VocabularyItems.ToList();
            var fromDbMap = fromDbList
                .GroupBy(x => x.Word)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (!fromDbMap.TryGetValue(item.Word, out var fromDb))
                {
                    dbContext.VocabularyItems.Add(item);
                    continue;
                }

                if (fromDb.Translation != item.Translation)
                {
                    fromDb.Translation = item.Translation;
                }

                if (fromDb.Example != item.Example)
                {
                    fromDb.Example = item.Example;
                }
            }

            dbContext.SaveChanges();
        }

        private static void SeedDemoContentEnglishOnly(LuminoDbContext dbContext)
        {
            var courseEnglish = EnsureCourse(
                dbContext,
                title: "English A1",
                description: "Basics: greetings, numbers, travel, simple phrases",
                isPublished: true);

            var topics = new List<TopicSeed>
            {
                new TopicSeed("Greetings", 1),
                new TopicSeed("Numbers", 2),
                new TopicSeed("Travel", 3),
                new TopicSeed("Food & Cafe", 4)
            };

            var topicMap = EnsureTopics(dbContext, courseEnglish.Id, topics);

            var lessons = new List<LessonSeed>
            {
                new LessonSeed(topicMap["Greetings"].Id, "Hello / Goodbye",
                    "Hello = Привіт\nGoodbye = До побачення\nPlease = Будь ласка\nThank you = Дякую", 1),
                new LessonSeed(topicMap["Greetings"].Id, "How are you?",
                    "How are you? = Як ти?\nI'm fine = У мене все добре\nAnd you? = А ти?", 2),
                new LessonSeed(topicMap["Greetings"].Id, "Introducing yourself",
                    "My name is ... = Мене звати ...\nNice to meet you = Приємно познайомитись", 3),
                new LessonSeed(topicMap["Greetings"].Id, "Polite words",
                    "Sorry = Пробач\nExcuse me = Перепрошую\nYou're welcome = Нема за що", 4),

                new LessonSeed(topicMap["Numbers"].Id, "Numbers 1-5",
                    "One, Two, Three, Four, Five", 1),
                new LessonSeed(topicMap["Numbers"].Id, "Numbers 6-10",
                    "Six, Seven, Eight, Nine, Ten", 2),

                new LessonSeed(topicMap["Travel"].Id, "At the airport",
                    "Airport = Аеропорт\nTicket = Квиток\nPassport = Паспорт\nWhere is the gate? = Де вихід?", 1),
                new LessonSeed(topicMap["Travel"].Id, "Asking directions",
                    "Where is ...? = Де ...?\nTurn left/right = Поверніть ліворуч/праворуч\nGo straight = Йдіть прямо", 2),

                new LessonSeed(topicMap["Food & Cafe"].Id, "In a cafe",
                    "Coffee = Кава\nTea = Чай\nMenu = Меню\nBill = Рахунок", 1),
                new LessonSeed(topicMap["Food & Cafe"].Id, "How much is it?",
                    "How much is it? = Скільки коштує?\nIt is ... = Це коштує ...\nCheap/Expensive = Дешево/Дорого", 2)
            };

            var lessonMap = EnsureLessons(dbContext, lessons);

            UpsertExercises(dbContext, lessonMap["Hello / Goodbye"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "Hello = ?", ToJsonStringArray("Привіт","До побачення","Дякую"), "Привіт", 1),
                new ExerciseSeed(ExerciseType.Input, "Write Ukrainian for: Goodbye", "{}", "До побачення", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "Please = ?", ToJsonStringArray("Будь ласка","Пробач","Нема за що"), "Будь ласка", 3),
                new ExerciseSeed(ExerciseType.Input, "Write Ukrainian for: Thank you", "{}", "Дякую", 4),
                new ExerciseSeed(ExerciseType.Match, "Match the pairs", ToJsonMatchPairs(
                    ("Hello", "Привіт"),
                    ("Goodbye", "До побачення"),
                    ("Please", "Будь ласка"),
                    ("Thank you", "Дякую")
                ), "{}", 5)
            });

            UpsertExercises(dbContext, lessonMap["How are you?"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "How are you? = ?", ToJsonStringArray("Як ти?","Де ти?","Хто ти?"), "Як ти?", 1),
                new ExerciseSeed(ExerciseType.Input, "Write English: У мене все добре", "{}", "I'm fine", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "And you? = ?", ToJsonStringArray("А ти?","І я","Ти добре?"), "А ти?", 3),
                new ExerciseSeed(ExerciseType.Input, "Write English: Як ти?", "{}", "How are you?", 4),
                new ExerciseSeed(ExerciseType.Match, "Match the phrases", ToJsonMatchPairs(
                    ("How are you?", "Як ти?"),
                    ("I'm fine", "У мене все добре"),
                    ("And you?", "А ти?")
                ), "{}", 5)
            });

            UpsertExercises(dbContext, lessonMap["Introducing yourself"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "My name is ... = ?", ToJsonStringArray("Мене звати ...","Я добре","Я тут"), "Мене звати ...", 1),
                new ExerciseSeed(ExerciseType.Input, "Write English: Мене звати Анна", "{}", "My name is Anna", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "Nice to meet you = ?", ToJsonStringArray("Приємно познайомитись","Добрий ранок","До побачення"), "Приємно познайомитись", 3),
                new ExerciseSeed(ExerciseType.Input, "Write English: Приємно познайомитись", "{}", "Nice to meet you", 4),
                new ExerciseSeed(ExerciseType.Match, "Match the phrases", ToJsonMatchPairs(
                    ("My name is ...", "Мене звати ..."),
                    ("Nice to meet you", "Приємно познайомитись")
                ), "{}", 5)
            });

            UpsertExercises(dbContext, lessonMap["Polite words"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "Sorry = ?", ToJsonStringArray("Пробач","Будь ласка","Дякую"), "Пробач", 1),
                new ExerciseSeed(ExerciseType.Input, "Write English: Перепрошую", "{}", "Excuse me", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "You're welcome = ?", ToJsonStringArray("Нема за що","До побачення","Привіт"), "Нема за що", 3),
                new ExerciseSeed(ExerciseType.Input, "Write Ukrainian for: Excuse me", "{}", "Перепрошую", 4),
                new ExerciseSeed(ExerciseType.Match, "Match the words", ToJsonMatchPairs(
                    ("Sorry", "Пробач"),
                    ("Excuse me", "Перепрошую"),
                    ("You're welcome", "Нема за що")
                ), "{}", 5)
            });

            UpsertExercises(dbContext, lessonMap["Numbers 1-5"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "Three = ?", ToJsonStringArray("Три","Чотири","П'ять"), "Три", 1),
                new ExerciseSeed(ExerciseType.Input, "Write English: Два", "{}", "Two", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "One = ?", ToJsonStringArray("Один","Нуль","П'ять"), "Один", 3),
                new ExerciseSeed(ExerciseType.Input, "Write English: П'ять", "{}", "Five", 4),
                new ExerciseSeed(ExerciseType.Match, "Match numbers", ToJsonMatchPairs(
                    ("One", "Один"),
                    ("Two", "Два"),
                    ("Three", "Три"),
                    ("Four", "Чотири"),
                    ("Five", "П'ять")
                ), "{}", 5)
            });

            UpsertExercises(dbContext, lessonMap["Numbers 6-10"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "Seven = ?", ToJsonStringArray("Сім","Шість","Вісім"), "Сім", 1),
                new ExerciseSeed(ExerciseType.Input, "Write English: Десять", "{}", "Ten", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "Nine = ?", ToJsonStringArray("Дев'ять","Вісім","Сім"), "Дев'ять", 3),
                new ExerciseSeed(ExerciseType.Input, "Write English: Шість", "{}", "Six", 4),
                new ExerciseSeed(ExerciseType.Match, "Match numbers", ToJsonMatchPairs(
                    ("Six", "Шість"),
                    ("Seven", "Сім"),
                    ("Eight", "Вісім"),
                    ("Nine", "Дев'ять"),
                    ("Ten", "Десять")
                ), "{}", 5)
            });

            UpsertExercises(dbContext, lessonMap["At the airport"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "Airport = ?", ToJsonStringArray("Аеропорт","Готель","Квиток"), "Аеропорт", 1),
                new ExerciseSeed(ExerciseType.Input, "Write English: паспорт", "{}", "passport", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "Ticket = ?", ToJsonStringArray("Квиток","Ключ","Меню"), "Квиток", 3),
                new ExerciseSeed(ExerciseType.Input, "Write Ukrainian for: passport", "{}", "паспорт", 4),
                new ExerciseSeed(ExerciseType.Match, "Match the words", ToJsonMatchPairs(
                    ("airport", "аеропорт"),
                    ("ticket", "квиток"),
                    ("passport", "паспорт")
                ), "{}", 5)
            });

            UpsertExercises(dbContext, lessonMap["Asking directions"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "Turn left = ?", ToJsonStringArray("Поверніть ліворуч","Поверніть праворуч","Йдіть прямо"), "Поверніть ліворуч", 1),
                new ExerciseSeed(ExerciseType.Input, "Write English: Йдіть прямо", "{}", "Go straight", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "Where is ...? = ?", ToJsonStringArray("Де ...?","Скільки коштує?","Котра година?"), "Де ...?", 3),
                new ExerciseSeed(ExerciseType.Input, "Write Ukrainian for: Turn right", "{}", "Поверніть праворуч", 4),
                new ExerciseSeed(ExerciseType.Match, "Match the phrases", ToJsonMatchPairs(
                    ("Turn left", "Поверніть ліворуч"),
                    ("Turn right", "Поверніть праворуч"),
                    ("Go straight", "Йдіть прямо")
                ), "{}", 5)
            });

            UpsertExercises(dbContext, lessonMap["In a cafe"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "Coffee = ?", ToJsonStringArray("Кава","Чай","Вода"), "Кава", 1),
                new ExerciseSeed(ExerciseType.Input, "Write Ukrainian for: menu", "{}", "меню", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "Bill = ?", ToJsonStringArray("Рахунок","Квиток","Ключ"), "Рахунок", 3),
                new ExerciseSeed(ExerciseType.Input, "Write English: Чай", "{}", "Tea", 4),
                new ExerciseSeed(ExerciseType.Match, "Match the words", ToJsonMatchPairs(
                    ("coffee", "кава"),
                    ("tea", "чай"),
                    ("menu", "меню"),
                    ("bill", "рахунок")
                ), "{}", 5)
            });

            UpsertExercises(dbContext, lessonMap["How much is it?"].Id, new List<ExerciseSeed>
            {
                new ExerciseSeed(ExerciseType.MultipleChoice, "How much is it? = ?", ToJsonStringArray("Скільки коштує?","Де ти?","Котра година?"), "Скільки коштує?", 1),
                new ExerciseSeed(ExerciseType.Input, "Write English: Це коштує 5", "{}", "It is 5", 2),
                new ExerciseSeed(ExerciseType.MultipleChoice, "Cheap = ?", ToJsonStringArray("Дешевий","Дорогий","Відкрито"), "Дешевий", 3),
                new ExerciseSeed(ExerciseType.Input, "Write English: Дорогий", "{}", "Expensive", 4),
                new ExerciseSeed(ExerciseType.Match, "Match the words", ToJsonMatchPairs(
                    ("price", "ціна"),
                    ("cheap", "дешевий"),
                    ("expensive", "дорогий")
                ), "{}", 5)
            });
        }

        private static Course EnsureCourse(LuminoDbContext dbContext, string title, string description, bool isPublished)
        {
            var fromDb = dbContext.Courses.FirstOrDefault(x => x.Title == title);

            if (fromDb == null)
            {
                var course = new Course
                {
                    Title = title,
                    Description = description,
                    IsPublished = isPublished
                };

                dbContext.Courses.Add(course);
                dbContext.SaveChanges();

                return course;
            }

            var changed = false;

            if (fromDb.Description != description)
            {
                fromDb.Description = description;
                changed = true;
            }

            if (fromDb.IsPublished != isPublished)
            {
                fromDb.IsPublished = isPublished;
                changed = true;
            }

            if (changed)
            {
                dbContext.SaveChanges();
            }

            return fromDb;
        }

        private static Dictionary<string, Topic> EnsureTopics(
            LuminoDbContext dbContext,
            int courseId,
            List<TopicSeed> seeds)
        {
            var fromDbList = dbContext.Topics
                .Where(x => x.CourseId == courseId)
                .ToList();

            var fromDbMap = fromDbList
                .GroupBy(x => x.Title)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var seed in seeds)
            {
                if (!fromDbMap.TryGetValue(seed.Title, out var fromDb))
                {
                    var topic = new Topic
                    {
                        CourseId = courseId,
                        Title = seed.Title,
                        Order = seed.Order
                    };

                    dbContext.Topics.Add(topic);
                    continue;
                }

                if (fromDb.Order != seed.Order)
                {
                    fromDb.Order = seed.Order;
                }
            }

            dbContext.SaveChanges();

            return dbContext.Topics
                .Where(x => x.CourseId == courseId)
                .ToList()
                .GroupBy(x => x.Title)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, Lesson> EnsureLessons(LuminoDbContext dbContext, List<LessonSeed> seeds)
        {
            var topicIds = seeds
                .Select(x => x.TopicId)
                .Distinct()
                .ToList();

            var fromDbList = dbContext.Lessons
                .Where(x => topicIds.Contains(x.TopicId))
                .ToList();

            var fromDbMap = fromDbList
                .GroupBy(x => $"{x.TopicId}:{x.Title}", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var seed in seeds)
            {
                var key = $"{seed.TopicId}:{seed.Title}";

                if (!fromDbMap.TryGetValue(key, out var fromDb))
                {
                    var lesson = new Lesson
                    {
                        TopicId = seed.TopicId,
                        Title = seed.Title,
                        Theory = seed.Theory,
                        Order = seed.Order
                    };

                    dbContext.Lessons.Add(lesson);
                    continue;
                }

                if (fromDb.Theory != seed.Theory)
                {
                    fromDb.Theory = seed.Theory;
                }

                if (fromDb.Order != seed.Order)
                {
                    fromDb.Order = seed.Order;
                }
            }

            dbContext.SaveChanges();

            return dbContext.Lessons
                .Where(x => topicIds.Contains(x.TopicId))
                .ToList()
                .GroupBy(x => $"{x.TopicId}:{x.Title}", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Value.Title, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }

        private static void UpsertExercises(LuminoDbContext dbContext, int lessonId, List<ExerciseSeed> seeds)
        {
            var fromDbList = dbContext.Exercises
                .Where(x => x.LessonId == lessonId)
                .ToList();

            var fromDbMap = fromDbList
                .GroupBy(x => x.Order)
                .ToDictionary(x => x.Key, x => x.First());

            foreach (var seed in seeds.OrderBy(x => x.Order))
            {
                if (!fromDbMap.TryGetValue(seed.Order, out var fromDb))
                {
                    var exercise = new Exercise
                    {
                        LessonId = lessonId,
                        Type = seed.Type,
                        Question = seed.Question,
                        Data = seed.Data,
                        CorrectAnswer = seed.CorrectAnswer,
                        Order = seed.Order
                    };

                    dbContext.Exercises.Add(exercise);
                    continue;
                }

                if (fromDb.Type != seed.Type)
                {
                    fromDb.Type = seed.Type;
                }

                if (fromDb.Question != seed.Question)
                {
                    fromDb.Question = seed.Question;
                }

                if (fromDb.Data != seed.Data)
                {
                    fromDb.Data = seed.Data;
                }

                if (fromDb.CorrectAnswer != seed.CorrectAnswer)
                {
                    fromDb.CorrectAnswer = seed.CorrectAnswer;
                }
            }

            dbContext.SaveChanges();
        }

        private static string ToJsonStringArray(params string[] items)
        {
            return JsonSerializer.Serialize(items);
        }

        private static string ToJsonMatchPairs(params (string Left, string Right)[] pairs)
        {
            var data = pairs
                .Select(x => new MatchPair { left = x.Left, right = x.Right })
                .ToList();

            return JsonSerializer.Serialize(data);
        }

        private class MatchPair
        {
            public string left { get; set; } = null!;
            public string right { get; set; } = null!;
        }

        private record SceneStepSeed(
            int Order,
            string Speaker,
            string Text,
            string StepType,
            string? MediaUrl = null,
            string? ChoicesJson = null);

        private record TopicSeed(string Title, int Order);

        private record LessonSeed(int TopicId, string Title, string Theory, int Order);

        private record ExerciseSeed(ExerciseType Type, string Question, string Data, string CorrectAnswer, int Order);
    }
}
