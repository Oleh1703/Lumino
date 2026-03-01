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
            LinkScenesToDefaultCourse(dbContext);

            SeedVocabularyLinks(dbContext);

            EnsureVocabularyBaseTranslations(dbContext);
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
                    Code = "sys.first_lesson",
                    Title = "First Lesson",
                    Description = "Complete your first lesson"
                },
                new Achievement
                {
                    Code = "sys.five_lessons",
                    Title = "5 Lessons Completed",
                    Description = "Complete 5 lessons"
                },
                new Achievement
                {
                    Code = "sys.perfect_lesson",
                    Title = "Perfect Lesson",
                    Description = "Complete a lesson without mistakes"
                },
                new Achievement
                {
                    Code = "sys.hundred_xp",
                    Title = "100 XP",
                    Description = "Earn 100 total score"
                },
                new Achievement
                {
                    Code = "sys.first_scene",
                    Title = "First Scene",
                    Description = "Complete your first scene"
                },
                new Achievement
                {
                    Code = "sys.five_scenes",
                    Title = "5 Scenes Completed",
                    Description = "Complete 5 scenes"
                },
                new Achievement
                {
                    Code = "sys.streak_starter",
                    Title = "Streak Starter",
                    Description = "Study 3 days in a row"
                },
                new Achievement
                {
                    Code = "sys.streak_7",
                    Title = "Streak 7",
                    Description = "Study 7 days in a row"
                },
                new Achievement
                {
                    Code = "sys.streak_30",
                    Title = "Streak 30",
                    Description = "Study 30 days in a row"
                },
                new Achievement
                {
                    Code = "sys.daily_goal",
                    Title = "Daily Goal",
                    Description = "Reach your daily goal"
                }
            };

            var fromDbList = dbContext.Achievements.ToList();
            var fromDbMap = fromDbList
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            var fromDbTitleMap = fromDbList
                .GroupBy(x => x.Title)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in achievements)
            {
                if (fromDbMap.TryGetValue(item.Code, out var fromDb))
                {
                    // Do not overwrite Title/Description here to keep admin edits.
                    continue;
                }

                // Backward-compatibility: old DB rows could exist without Code.
                if (fromDbTitleMap.TryGetValue(item.Title, out var byTitle) && string.IsNullOrWhiteSpace(byTitle.Code))
                {
                    byTitle.Code = item.Code;
                    continue;
                }

                dbContext.Achievements.Add(item);
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
                    AudioUrl = null,
                    Order = 1
                },
                new Scene
                {
                    Title = "Airport check-in",
                    Description = "Check in for your flight",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null,
                    Order = 2
                },
                new Scene
                {
                    Title = "Hotel booking",
                    Description = "Book a room at a hotel reception",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null,
                    Order = 3
                },
                new Scene
                {
                    Title = "Asking directions",
                    Description = "Ask how to get to a place in the city",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null,
                    Order = 4
                },
                new Scene
                {
                    Title = "Shopping",
                    Description = "Buy something in a store and ask the price",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null,
                    Order = 5
                },
                new Scene
                {
                    Title = "Small talk",
                    Description = "Introduce yourself and keep a short conversation",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null,
                    Order = 6
                },
                new Scene
                {
                    Title = "Restaurant reservation",
                    Description = "Reserve a table at a restaurant",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null,
                    Order = 7
                },
                new Scene
                {
                    Title = "Doctor visit",
                    Description = "Describe your symptoms at a doctor",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null,
                    Order = 8
                },
                new Scene
                {
                    Title = "Public transport",
                    Description = "Buy a ticket and ask about the bus",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null,
                    Order = 9
                },
                new Scene
                {
                    Title = "Job interview",
                    Description = "Answer basic questions in an interview",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null,
                    Order = 10
                }
            };

            var fromDbList = dbContext.Scenes.ToList();
            var fromDbMap = fromDbList
                .GroupBy(x => x.Title)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            var orderUpdates = new Dictionary<int, int>();
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

                if (!string.IsNullOrWhiteSpace(item.BackgroundUrl) && fromDb.BackgroundUrl != item.BackgroundUrl)
                {
                    fromDb.BackgroundUrl = item.BackgroundUrl;
                }

                if (!string.IsNullOrWhiteSpace(item.AudioUrl) && fromDb.AudioUrl != item.AudioUrl)
                {
                    fromDb.AudioUrl = item.AudioUrl;
                }
                if (item.Order > 0 && fromDb.Order != item.Order)
                {
                    // Safe update for unique index (CourseId, Order) where Order > 0:
                    // if the scene is already linked to a course, change order in 2 steps to avoid collisions.
                    if (fromDb.CourseId != null && fromDb.CourseId > 0 && fromDb.Order > 0)
                    {
                        fromDb.Order = 0;
                        orderUpdates[fromDb.Id] = item.Order;
                    }
                    else
                    {
                        fromDb.Order = item.Order;
                    }
                }
            }

            dbContext.SaveChanges();

            if (orderUpdates.Count > 0)
            {
                var ids = orderUpdates.Keys.ToList();

                var toFix = dbContext.Scenes
                    .Where(x => ids.Contains(x.Id))
                    .ToList();

                foreach (var s in toFix)
                {
                    if (orderUpdates.TryGetValue(s.Id, out var newOrder))
                    {
                        s.Order = newOrder;
                    }
                }

                dbContext.SaveChanges();
            }

            var sceneMap = dbContext.Scenes
                .ToList()
                .GroupBy(x => x.Title)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            var stepsBySceneTitle = new Dictionary<string, List<SceneStepSeed>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Cafe order"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "Barista", "Hi! What would you like?", "Line", null, null),
                    new SceneStepSeed(2, "You", "I'd like a coffee, please.", "Line", null, null),
                    new SceneStepSeed(3, "Quiz", "What did you order?", "Choice", null, "[{\"text\": \"Coffee\", \"isCorrect\": true}, {\"text\": \"Tea\", \"isCorrect\": false}, {\"text\": \"Water\", \"isCorrect\": false}]"),
                    new SceneStepSeed(4, "Barista", "Sure. Anything else?", "Line", null, null),
                    new SceneStepSeed(5, "Quiz", "Do you want anything else?", "Choice", null, "[{\"text\": \"No\", \"isCorrect\": true}, {\"text\": \"Yes\", \"isCorrect\": false}]"),
                    new SceneStepSeed(6, "Quiz", "Type: Thank you", "Input", null, "{\"correctAnswer\": \"Thank you\", \"acceptableAnswers\": [\"Thanks\", \"Thanks!\"]}")
                },
                ["Airport check-in"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "Staff", "Hello. Can I see your passport?", "Line", null, null),
                    new SceneStepSeed(2, "You", "Yes, here you go.", "Line", null, null),
                    new SceneStepSeed(3, "Quiz", "Type the key word: passport", "Input", null, "{\"correctAnswer\": \"passport\", \"acceptableAnswers\": [\"my passport\"]}"),
                    new SceneStepSeed(4, "Staff", "Do you have any luggage?", "Line", null, null),
                    new SceneStepSeed(5, "Quiz", "How many bags do you have?", "Choice", null, "[{\"text\": \"One small bag\", \"isCorrect\": true}, {\"text\": \"Three suitcases\", \"isCorrect\": false}, {\"text\": \"None\", \"isCorrect\": false}]"),
                    new SceneStepSeed(6, "You", "Just a small bag.", "Line", null, null)
                },
                ["Hotel booking"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "Receptionist", "Hello! Do you have a reservation?", "Line", null, null),
                    new SceneStepSeed(2, "Quiz", "What does \"reservation\" mean here?", "Choice", null, "[{\"text\": \"Booking\", \"isCorrect\": true}, {\"text\": \"Restaurant\", \"isCorrect\": false}, {\"text\": \"Passport\", \"isCorrect\": false}]"),
                    new SceneStepSeed(3, "You", "Yes, it's under my name.", "Line", null, null),
                    new SceneStepSeed(4, "Quiz", "Type: key", "Input", null, "{\"correctAnswer\": \"key\", \"acceptableAnswers\": [\"room key\"]}"),
                    new SceneStepSeed(5, "Receptionist", "Great. Here is your key.", "Line", null, null),
                    new SceneStepSeed(6, "You", "Thank you!", "Line", null, null)
                },
                ["Asking directions"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "You", "Excuse me, where is the station?", "Line", null, null),
                    new SceneStepSeed(2, "Quiz", "Where do you want to go?", "Choice", null, "[{\"text\": \"To the station\", \"isCorrect\": true}, {\"text\": \"To the hotel\", \"isCorrect\": false}, {\"text\": \"To the airport\", \"isCorrect\": false}]"),
                    new SceneStepSeed(3, "Person", "Go straight and turn left.", "Line", null, null),
                    new SceneStepSeed(4, "Quiz", "Type: left", "Input", null, "{\"correctAnswer\": \"left\", \"acceptableAnswers\": [\"turn left\"]}"),
                    new SceneStepSeed(5, "You", "Thanks a lot!", "Line", null, null),
                    new SceneStepSeed(6, "Person", "You're welcome.", "Line", null, null)
                },
                ["Shopping"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "You", "How much is this?", "Line", null, null),
                    new SceneStepSeed(2, "Seller", "It's ten dollars.", "Line", null, null),
                    new SceneStepSeed(3, "Quiz", "How much is it?", "Choice", null, "[{\"text\": \"Ten dollars\", \"isCorrect\": true}, {\"text\": \"Five dollars\", \"isCorrect\": false}, {\"text\": \"Twenty dollars\", \"isCorrect\": false}]"),
                    new SceneStepSeed(4, "You", "That's cheap! I'll take it.", "Line", null, null),
                    new SceneStepSeed(5, "Quiz", "Type: I'll take it", "Input", null, "{\"correctAnswer\": \"I'll take it\", \"acceptableAnswers\": [\"I will take it\"]}"),
                    new SceneStepSeed(6, "Seller", "Great choice!", "Line", null, null)
                },
                ["Small talk"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "You", "Hi! My name is Alex.", "Line", null, null),
                    new SceneStepSeed(2, "Person", "Nice to meet you, Alex!", "Line", null, null),
                    new SceneStepSeed(3, "Quiz", "What is your name?", "Choice", null, "[{\"text\": \"Alex\", \"isCorrect\": true}, {\"text\": \"John\", \"isCorrect\": false}, {\"text\": \"Kate\", \"isCorrect\": false}]"),
                    new SceneStepSeed(4, "Quiz", "Type: Nice to meet you", "Input", null, "{\"correctAnswer\": \"Nice to meet you\", \"acceptableAnswers\": [\"Nice to meet you!\"]}"),
                    new SceneStepSeed(5, "You", "Nice to meet you too.", "Line", null, null),
                    new SceneStepSeed(6, "Person", "How are you?", "Line", null, null)
                },
                ["Restaurant reservation"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "Host", "Hello! Do you have a reservation?", "Line", null, null),
                    new SceneStepSeed(2, "You", "Yes, a table for two, please.", "Line", null, null),
                    new SceneStepSeed(3, "Quiz", "How many people?", "Choice", null, "[{\"text\": \"Two\", \"isCorrect\": true}, {\"text\": \"Five\", \"isCorrect\": false}, {\"text\": \"One\", \"isCorrect\": false}]"),
                    new SceneStepSeed(4, "Host", "Great. What time would you like?", "Line", null, null),
                    new SceneStepSeed(5, "Quiz", "Type: at seven", "Input", null, "{\"correctAnswer\": \"at seven\", \"acceptableAnswers\": [\"7 pm\", \"at 7\"]}"),
                    new SceneStepSeed(6, "You", "At seven, thank you.", "Line", null, null)
                },
                ["Doctor visit"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "Doctor", "Hello. What seems to be the problem?", "Line", null, null),
                    new SceneStepSeed(2, "You", "I have a headache.", "Line", null, null),
                    new SceneStepSeed(3, "Quiz", "What do you have?", "Choice", null, "[{\"text\": \"A headache\", \"isCorrect\": true}, {\"text\": \"A ticket\", \"isCorrect\": false}, {\"text\": \"A coffee\", \"isCorrect\": false}]"),
                    new SceneStepSeed(4, "Doctor", "How long have you had it?", "Line", null, null),
                    new SceneStepSeed(5, "Quiz", "Type: two days", "Input", null, "{\"correctAnswer\": \"two days\", \"acceptableAnswers\": [\"2 days\", \"for two days\"]}"),
                    new SceneStepSeed(6, "Doctor", "Ok. Please rest and drink water.", "Line", null, null)
                },
                ["Public transport"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "You", "Excuse me, where can I buy a ticket?", "Line", null, null),
                    new SceneStepSeed(2, "Staff", "You can buy it here.", "Line", null, null),
                    new SceneStepSeed(3, "Quiz", "Where can you buy a ticket?", "Choice", null, "[{\"text\": \"Here\", \"isCorrect\": true}, {\"text\": \"At the hotel\", \"isCorrect\": false}, {\"text\": \"At the cafe\", \"isCorrect\": false}]"),
                    new SceneStepSeed(4, "Staff", "One-way or return?", "Line", null, null),
                    new SceneStepSeed(5, "Quiz", "Type: one-way", "Input", null, "{\"correctAnswer\": \"one-way\", \"acceptableAnswers\": [\"one way\"]}"),
                    new SceneStepSeed(6, "You", "One-way, please.", "Line", null, null)
                },
                ["Job interview"] = new List<SceneStepSeed>
                {
                    new SceneStepSeed(1, "Interviewer", "Welcome. Can you tell me about yourself?", "Line", null, null),
                    new SceneStepSeed(2, "You", "I'm a student and I like learning languages.", "Line", null, null),
                    new SceneStepSeed(3, "Quiz", "Who are you?", "Choice", null, "[{\"text\": \"A student\", \"isCorrect\": true}, {\"text\": \"A pilot\", \"isCorrect\": false}, {\"text\": \"A doctor\", \"isCorrect\": false}]"),
                    new SceneStepSeed(4, "Interviewer", "Why do you want this job?", "Line", null, null),
                    new SceneStepSeed(5, "Quiz", "Type: I want to grow", "Input", null, "{\"correctAnswer\": \"I want to grow\", \"acceptableAnswers\": [\"I want to improve\", \"I want to learn\"]}"),
                    new SceneStepSeed(6, "Interviewer", "Great. Thank you for coming.", "Line", null, null)
                }
            };

            var fromStepsList = dbContext.SceneSteps.ToList();
            var fromStepsMap = fromStepsList
                .GroupBy(x => $"{x.SceneId}:{x.Order}")
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var kv in stepsBySceneTitle)
            {
                if (!sceneMap.TryGetValue(kv.Key, out var scene))
                {
                    continue;
                }

                foreach (var step in kv.Value)
                {
                    var key = $"{scene.Id}:{step.Order}";

                    if (!fromStepsMap.TryGetValue(key, out var fromDb))
                    {
                        dbContext.SceneSteps.Add(new SceneStep
                        {
                            SceneId = scene.Id,
                            Order = step.Order,
                            Speaker = step.Speaker,
                            Text = step.Text,
                            StepType = step.StepType,
                            MediaUrl = step.MediaUrl,
                            ChoicesJson = step.ChoicesJson
                        });

                        continue;
                    }

                    if (fromDb.Speaker != step.Speaker)
                    {
                        fromDb.Speaker = step.Speaker;
                    }

                    if (fromDb.Text != step.Text)
                    {
                        fromDb.Text = step.Text;
                    }

                    if (fromDb.StepType != step.StepType)
                    {
                        fromDb.StepType = step.StepType;
                    }

                    if (!string.IsNullOrWhiteSpace(step.MediaUrl) && fromDb.MediaUrl != step.MediaUrl)
                    {
                        fromDb.MediaUrl = step.MediaUrl;
                    }

                    if (!string.IsNullOrWhiteSpace(step.ChoicesJson) && fromDb.ChoicesJson != step.ChoicesJson)
                    {
                        fromDb.ChoicesJson = step.ChoicesJson;
                    }
                }
            }

            dbContext.SaveChanges();
        }

        private static void LinkScenesToDefaultCourse(LuminoDbContext dbContext)
        {
            // link scenes to the default course (published first, otherwise any).
            // Safe to call multiple times.
            var defaultCourse = dbContext.Courses.FirstOrDefault(x => x.Title == "English A1")
                ?? dbContext.Courses.FirstOrDefault(x => x.IsPublished)
                ?? dbContext.Courses.FirstOrDefault();
            if (defaultCourse == null)
            {
                return;
            }

            var scenes = dbContext.Scenes.ToList();
            bool changed = false;

            foreach (var scene in scenes)
            {
                // do not overwrite if already linked
                if (scene.CourseId != null && scene.CourseId > 0)
                {
                    continue;
                }

                scene.CourseId = defaultCourse.Id;
                changed = true;
            }

            if (changed)
            {
                dbContext.SaveChanges();
            }
        }

        private static void SeedVocabulary(LuminoDbContext dbContext)
        {
            var items = new List<VocabularyItem>
            {
                new VocabularyItem { Word = "hello", Translation = "привіт", Example = "Hello! How are you?", Transcription = "/həˈloʊ/", ExamplesJson = ToJsonStringArray("Hello! How are you?"), SynonymsJson = ToJsonStringArray("hi — Hi! How are you?", "hey — Hey! Nice to see you.") },
                new VocabularyItem { Word = "goodbye", Translation = "до побачення", Example = "Goodbye! See you soon.", Transcription = "/ˌɡʊdˈbaɪ/", ExamplesJson = ToJsonStringArray("Goodbye! See you soon."), SynonymsJson = ToJsonStringArray("bye — Bye! See you later.", "farewell — Farewell, my friend.") },
                new VocabularyItem { Word = "please", Translation = "будь ласка", Example = "Please, help me.", Transcription = "/pliːz/", ExamplesJson = ToJsonStringArray("Please, help me."), SynonymsJson = ToJsonStringArray("kindly — Kindly sit down.", "if you please — Open the window, if you please.") },
                new VocabularyItem { Word = "thank you", Translation = "дякую", Example = "Thank you for your help.", Transcription = "/ˈθæŋk juː/", ExamplesJson = ToJsonStringArray("Thank you for your help."), SynonymsJson = ToJsonStringArray("thanks — Thanks for your help.", "much appreciated — Your support is much appreciated.") },
                new VocabularyItem { Word = "yes", Translation = "так", Example = "Yes, I agree.", Transcription = "/jɛs/", ExamplesJson = ToJsonStringArray("Yes, I agree."), SynonymsJson = ToJsonStringArray("yeah — Yeah, sure.", "certainly — Certainly, I agree.") },
                new VocabularyItem { Word = "no", Translation = "ні", Example = "No, I don't know.", Transcription = "/noʊ/", ExamplesJson = ToJsonStringArray("No, I don't know."), SynonymsJson = ToJsonStringArray("nope — Nope, not today.", "not really — Not really, I don't think so.") },
                new VocabularyItem { Word = "sorry", Translation = "пробач", Example = "Sorry, I'm late.", Transcription = "/ˈsɑːri/", ExamplesJson = ToJsonStringArray("Sorry, I'm late."), SynonymsJson = ToJsonStringArray("apologies — Apologies for the mistake.", "my bad — My bad, I forgot.") },
                new VocabularyItem { Word = "excuse me", Translation = "перепрошую", Example = "Excuse me, where is the station?", Transcription = "/ɪkˈskjuːz miː/", ExamplesJson = ToJsonStringArray("Excuse me, where is the station?"), SynonymsJson = ToJsonStringArray("pardon me — Pardon me, can I pass?", "sorry — Sorry, could you repeat that?") },
                new VocabularyItem { Word = "welcome", Translation = "ласкаво просимо", Example = "Welcome to our city!", Transcription = "/ˈwɛlkəm/", ExamplesJson = ToJsonStringArray("Welcome to our city!"), SynonymsJson = ToJsonStringArray("you’re welcome — You’re welcome!", "no problem — No problem at all.") },
                new VocabularyItem { Word = "good morning", Translation = "доброго ранку / добрий ранок", Example = "Good morning! Have a nice day.", Transcription = "/ɡʊd ˈmɔːrnɪŋ/", ExamplesJson = ToJsonStringArray("Good morning! Have a nice day."), SynonymsJson = ToJsonStringArray("morning — Morning!", "good day — Good day to you.") },
                new VocabularyItem { Word = "good evening", Translation = "доброго вечора / добрий вечір", Example = "Good evening! Nice to see you.", Transcription = "/ɡʊd ˈiːvnɪŋ/", ExamplesJson = ToJsonStringArray("Good evening! Nice to see you."), SynonymsJson = ToJsonStringArray("evening — Evening!", "good night — Good night! (often used when leaving)") },
                new VocabularyItem { Word = "one", Translation = "один", Example = "One plus one is two.", Transcription = "/wʌn/", ExamplesJson = ToJsonStringArray("One plus one is two.") },
                new VocabularyItem { Word = "two", Translation = "два", Example = "Two cups of tea, please.", Transcription = "/tuː/", ExamplesJson = ToJsonStringArray("Two cups of tea, please.") },
                new VocabularyItem { Word = "three", Translation = "три", Example = "Three days ago.", Transcription = "/θriː/", ExamplesJson = ToJsonStringArray("Three days ago.") },
                new VocabularyItem { Word = "four", Translation = "чотири", Example = "Four people.", Transcription = "/fɔːr/", ExamplesJson = ToJsonStringArray("Four people.") },
                new VocabularyItem { Word = "five", Translation = "п'ять", Example = "Five minutes.", Transcription = "/faɪv/", ExamplesJson = ToJsonStringArray("Five minutes.") },
                new VocabularyItem { Word = "six", Translation = "шість", Example = "Six tickets.", Transcription = "/sɪks/", ExamplesJson = ToJsonStringArray("Six tickets.") },
                new VocabularyItem { Word = "seven", Translation = "сім", Example = "Seven o'clock.", Transcription = "/ˈsɛvən/", ExamplesJson = ToJsonStringArray("Seven o'clock.") },
                new VocabularyItem { Word = "eight", Translation = "вісім", Example = "Eight apples.", Transcription = "/eɪt/", ExamplesJson = ToJsonStringArray("Eight apples.") },
                new VocabularyItem { Word = "nine", Translation = "дев'ять", Example = "Nine rooms.", Transcription = "/naɪn/", ExamplesJson = ToJsonStringArray("Nine rooms.") },
                new VocabularyItem { Word = "ten", Translation = "десять", Example = "Ten dollars.", Transcription = "/tɛn/", ExamplesJson = ToJsonStringArray("Ten dollars.") },

                new VocabularyItem { Word = "water", Translation = "вода", Example = "I want water.", Transcription = "/ˈwɔːtər/", ExamplesJson = ToJsonStringArray("I want water."), SynonymsJson = ToJsonStringArray("H2O — Water (H2O) is essential.") },
                new VocabularyItem { Word = "coffee", Translation = "кава", Example = "Coffee, please.", Transcription = "/ˈkɔːfi/", ExamplesJson = ToJsonStringArray("Coffee, please."), SynonymsJson = ToJsonStringArray("java — I need a cup of java.", "espresso — I'll take an espresso, please.") },
                new VocabularyItem { Word = "tea", Translation = "чай", Example = "Tea is hot.", Transcription = "/tiː/", ExamplesJson = ToJsonStringArray("Tea is hot."), SynonymsJson = ToJsonStringArray("cuppa — Fancy a cuppa?", "brew — Let's have a brew.") },
                new VocabularyItem { Word = "bread", Translation = "хліб", Example = "I like bread.", Transcription = "/brɛd/", ExamplesJson = ToJsonStringArray("I like bread."), SynonymsJson = ToJsonStringArray("loaf — A loaf of bread, please.", "buns — Fresh buns are tasty.") },
                new VocabularyItem { Word = "milk", Translation = "молоко", Example = "Milk in my coffee, please.", Transcription = "/mɪlk/", ExamplesJson = ToJsonStringArray("Milk in my coffee, please."), SynonymsJson = ToJsonStringArray("dairy milk — Dairy milk is in the fridge.", "cow’s milk — Do you prefer cow’s milk?") },
                new VocabularyItem { Word = "sugar", Translation = "цукор", Example = "No sugar, please.", Transcription = "/ˈʃʊɡər/", ExamplesJson = ToJsonStringArray("No sugar, please."), SynonymsJson = ToJsonStringArray("sweetener — Add some sweetener.", "sucrose — Sugar is mostly sucrose.") },
                new VocabularyItem { Word = "salt", Translation = "сіль", Example = "Add some salt.", Transcription = "/sɔːlt/", ExamplesJson = ToJsonStringArray("Add some salt."), SynonymsJson = ToJsonStringArray("seasoning — Add a little seasoning.", "table salt — Pass the table salt, please.") },
                new VocabularyItem { Word = "menu", Translation = "меню", Example = "Can I see the menu?", Transcription = "/ˈmɛnjuː/", ExamplesJson = ToJsonStringArray("Can I see the menu?"), SynonymsJson = ToJsonStringArray("list — Check the list of dishes.", "card — Can I see the menu card?") },
                new VocabularyItem { Word = "bill", Translation = "рахунок", Example = "Can I have the bill, please?", Transcription = "/bɪl/", ExamplesJson = ToJsonStringArray("Can I have the bill, please?"), SynonymsJson = ToJsonStringArray("check — Could we get the check?", "receipt — I need a receipt, please.") },

                new VocabularyItem { Word = "airport", Translation = "аеропорт", Example = "The airport is big.", Transcription = "/ˈɛrˌpɔːrt/", ExamplesJson = ToJsonStringArray("The airport is big."), SynonymsJson = ToJsonStringArray("airfield — The airfield is nearby.", "terminal — Meet me at the terminal.") },
                new VocabularyItem { Word = "ticket", Translation = "квиток", Example = "I have a ticket.", Transcription = "/ˈtɪkɪt/", ExamplesJson = ToJsonStringArray("I have a ticket."), SynonymsJson = ToJsonStringArray("pass — Show me your pass.", "fare ticket — Buy a fare ticket.") },
                new VocabularyItem { Word = "passport", Translation = "паспорт", Example = "Show me your passport.", Transcription = "/ˈpæsˌpɔːrt/", ExamplesJson = ToJsonStringArray("Show me your passport."), SynonymsJson = ToJsonStringArray("travel document — Keep your travel document safe.", "ID — Show your passport or ID.") },
                new VocabularyItem { Word = "plane", Translation = "літак", Example = "The plane is on time.", Transcription = "/pleɪn/", ExamplesJson = ToJsonStringArray("The plane is on time."), SynonymsJson = ToJsonStringArray("aircraft — The aircraft is ready.", "airplane — The airplane is boarding.") },
                new VocabularyItem { Word = "train", Translation = "поїзд", Example = "The train is fast.", Transcription = "/treɪn/", ExamplesJson = ToJsonStringArray("The train is fast."), SynonymsJson = ToJsonStringArray("railway — The railway is busy.", "rail train — Catch the rail train.") },
                new VocabularyItem { Word = "bus", Translation = "автобус", Example = "The bus is late.", Transcription = "/bʌs/", ExamplesJson = ToJsonStringArray("The bus is late."), SynonymsJson = ToJsonStringArray("coach — Take the coach downtown.", "shuttle — The shuttle bus arrives soon.") },
                new VocabularyItem { Word = "station", Translation = "станція", Example = "Where is the station?", Transcription = "/ˈsteɪʃən/", ExamplesJson = ToJsonStringArray("Where is the station?"), SynonymsJson = ToJsonStringArray("terminal — The terminal is crowded.", "stop — Next stop is the station.") },
                new VocabularyItem { Word = "hotel", Translation = "готель", Example = "The hotel is nice.", Transcription = "/hoʊˈtɛl/", ExamplesJson = ToJsonStringArray("The hotel is nice."), SynonymsJson = ToJsonStringArray("inn — We stayed at an inn.", "lodging — The lodging is comfortable.") },
                new VocabularyItem { Word = "room", Translation = "кімната", Example = "This is my room.", Transcription = "/ruːm/", ExamplesJson = ToJsonStringArray("This is my room."), SynonymsJson = ToJsonStringArray("chamber — The chamber is clean.", "suite — We booked a suite.") },
                new VocabularyItem { Word = "key", Translation = "ключ", Example = "Here is your key.", Transcription = "/kiː/", ExamplesJson = ToJsonStringArray("Here is your key."), SynonymsJson = ToJsonStringArray("passkey — Here's the passkey.", "card key — Use the card key.") },

                new VocabularyItem { Word = "where", Translation = "де", Example = "Where are you?", Transcription = "/wɛr/", ExamplesJson = ToJsonStringArray("Where are you?"), SynonymsJson = ToJsonStringArray("in what place — Where (in what place) is it?") },
                new VocabularyItem { Word = "when", Translation = "коли", Example = "When do we leave?", Transcription = "/wɛn/", ExamplesJson = ToJsonStringArray("When do we leave?"), SynonymsJson = ToJsonStringArray("at what time — When (at what time) does it start?") },
                new VocabularyItem { Word = "who", Translation = "хто", Example = "Who is that?", Transcription = "/huː/", ExamplesJson = ToJsonStringArray("Who is that?"), SynonymsJson = ToJsonStringArray("which person — Who (which person) called?") },
                new VocabularyItem { Word = "what", Translation = "що", Example = "What is this?", Transcription = "/wʌt/", ExamplesJson = ToJsonStringArray("What is this?"), SynonymsJson = ToJsonStringArray("which thing — What (which thing) is this?") },
                new VocabularyItem { Word = "how", Translation = "як", Example = "How are you?", Transcription = "/haʊ/", ExamplesJson = ToJsonStringArray("How are you?"), SynonymsJson = ToJsonStringArray("in what way — How (in what way) did you do it?") },
                new VocabularyItem { Word = "why", Translation = "чому", Example = "Why are you sad?", Transcription = "/waɪ/", ExamplesJson = ToJsonStringArray("Why are you sad?"), SynonymsJson = ToJsonStringArray("for what reason — Why (for what reason) are you late?") },

                new VocabularyItem { Word = "open", Translation = "відкрито", Example = "The shop is open.", Transcription = "/ˈoʊpən/", ExamplesJson = ToJsonStringArray("The shop is open."), SynonymsJson = ToJsonStringArray("unlocked — The door is unlocked.", "available — The shop is open and available.") },
                new VocabularyItem { Word = "closed", Translation = "закрито", Example = "The shop is closed.", Transcription = "/kloʊzd/", ExamplesJson = ToJsonStringArray("The shop is closed."), SynonymsJson = ToJsonStringArray("shut — The shop is shut.", "not open — The office is not open today.") },
                new VocabularyItem { Word = "left", Translation = "ліворуч", Example = "Turn left.", Transcription = "/lɛft/", ExamplesJson = ToJsonStringArray("Turn left."), SynonymsJson = ToJsonStringArray("to the left — Turn to the left.", "port — The ship turned port (left).") },
                new VocabularyItem { Word = "right", Translation = "праворуч", Example = "Turn right.", Transcription = "/raɪt/", ExamplesJson = ToJsonStringArray("Turn right."), SynonymsJson = ToJsonStringArray("correct — That's right (correct).", "starboard — Turn starboard (right).") },
                new VocabularyItem { Word = "straight", Translation = "прямо", Example = "Go straight.", Transcription = "/streɪt/", ExamplesJson = ToJsonStringArray("Go straight."), SynonymsJson = ToJsonStringArray("directly — Go directly ahead.", "forward — Walk forward.") },

                new VocabularyItem { Word = "how much", Translation = "скільки коштує", Example = "How much is it?", Transcription = "/haʊ mʌtʃ/", ExamplesJson = ToJsonStringArray("How much is it?"), SynonymsJson = ToJsonStringArray("what amount — How much (what amount) is it?") },
                new VocabularyItem { Word = "price", Translation = "ціна", Example = "The price is high.", Transcription = "/praɪs/", ExamplesJson = ToJsonStringArray("The price is high."), SynonymsJson = ToJsonStringArray("cost — The cost is high.", "rate — What's the rate per night?") },
                new VocabularyItem { Word = "cheap", Translation = "дешевий", Example = "This is cheap.", Transcription = "/tʃiːp/", ExamplesJson = ToJsonStringArray("This is cheap."), SynonymsJson = ToJsonStringArray("inexpensive — It's inexpensive.", "low-cost — A low-cost option.") },
                new VocabularyItem { Word = "expensive", Translation = "дорогий", Example = "This is expensive.", Transcription = "/ɪkˈspɛnsɪv/", ExamplesJson = ToJsonStringArray("This is expensive."), SynonymsJson = ToJsonStringArray("costly — That's costly.", "pricey — The meal is pricey.") },

                new VocabularyItem { Word = "time", Translation = "час", Example = "What time is it?", Transcription = "/taɪm/", ExamplesJson = ToJsonStringArray("What time is it?"), SynonymsJson = ToJsonStringArray("moment — Give me a moment.", "hour — What time (hour) is it?") },
                new VocabularyItem { Word = "today", Translation = "сьогодні", Example = "Today is Monday.", Transcription = "/təˈdeɪ/", ExamplesJson = ToJsonStringArray("Today is Monday."), SynonymsJson = ToJsonStringArray("this day — Today (this day) is sunny.", "nowadays — Today, many people...") },
                new VocabularyItem { Word = "tomorrow", Translation = "завтра", Example = "See you tomorrow.", Transcription = "/təˈmɑːroʊ/", ExamplesJson = ToJsonStringArray("See you tomorrow."), SynonymsJson = ToJsonStringArray("next day — Tomorrow (next day) we'll travel.", "the following day — The following day was better.") },
                new VocabularyItem { Word = "yesterday", Translation = "вчора", Example = "Yesterday was cold.", Transcription = "/ˈjɛstərdeɪ/", ExamplesJson = ToJsonStringArray("Yesterday was cold.") }
            };

            var extraTranslationsByWord = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // Додаткові переклади (крім основного поля Translation).
                // Порядок важливий: він збережеться у VocabularyItemTranslations.Order.
                ["sorry"] = new List<string> { "вибач", "перепрошую" },
                ["ticket"] = new List<string> { "білет" },
                ["station"] = new List<string> { "вокзал" },
                ["room"] = new List<string> { "номер", "кімната" },
                ["cheap"] = new List<string> { "дешево", "недорогий" },
                ["expensive"] = new List<string> { "дорого", "коштовний" }
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

                // Донаповнюємо нові поля, не стираючи те, що могло бути додано через адмінку.
                // Правило: якщо в БД поле порожнє, а в сіді є значення — переносимо його в БД.
                if (string.IsNullOrWhiteSpace(fromDb.Transcription) && !string.IsNullOrWhiteSpace(item.Transcription))
                {
                    fromDb.Transcription = item.Transcription;
                }

                if (string.IsNullOrWhiteSpace(fromDb.Gender) && !string.IsNullOrWhiteSpace(item.Gender))
                {
                    fromDb.Gender = item.Gender;
                }

                if (string.IsNullOrWhiteSpace(fromDb.PartOfSpeech) && !string.IsNullOrWhiteSpace(item.PartOfSpeech))
                {
                    fromDb.PartOfSpeech = item.PartOfSpeech;
                }

                if (string.IsNullOrWhiteSpace(fromDb.Definition) && !string.IsNullOrWhiteSpace(item.Definition))
                {
                    fromDb.Definition = item.Definition;
                }

                if (string.IsNullOrWhiteSpace(fromDb.ExamplesJson) && !string.IsNullOrWhiteSpace(item.ExamplesJson))
                {
                    fromDb.ExamplesJson = item.ExamplesJson;
                }

                if (string.IsNullOrWhiteSpace(fromDb.SynonymsJson) && !string.IsNullOrWhiteSpace(item.SynonymsJson))
                {
                    fromDb.SynonymsJson = item.SynonymsJson;
                }

                if (string.IsNullOrWhiteSpace(fromDb.IdiomsJson) && !string.IsNullOrWhiteSpace(item.IdiomsJson))
                {
                    fromDb.IdiomsJson = item.IdiomsJson;
                }
            }

            dbContext.SaveChanges();

            UpsertVocabularyTranslations(dbContext, items, extraTranslationsByWord);
        }


        private static void UpsertVocabularyTranslations(
            LuminoDbContext dbContext,
            List<VocabularyItem> seededItems,
            Dictionary<string, List<string>> extraTranslationsByWord)
        {
            // 1) Мапа слово -> entity з БД
            var fromDbList = dbContext.VocabularyItems.ToList();
            var fromDbMap = fromDbList
                .GroupBy(x => x.Word)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            // 2) Існуючі переклади з БД
            var existing = dbContext.VocabularyItemTranslations.ToList();
            var existingByItemId = existing
                .GroupBy(x => x.VocabularyItemId)
                .ToDictionary(x => x.Key, x => x.OrderBy(t => t.Order).ToList());

            foreach (var seed in seededItems)
            {
                if (!fromDbMap.TryGetValue(seed.Word, out var entity))
                {
                    continue;
                }

                var desired = new List<string>();

                if (!string.IsNullOrWhiteSpace(seed.Translation))
                {
                    desired.Add(seed.Translation.Trim());
                }

                if (extraTranslationsByWord.TryGetValue(seed.Word, out var extras))
                {
                    foreach (var tr in extras)
                    {
                        if (!string.IsNullOrWhiteSpace(tr))
                        {
                            desired.Add(tr.Trim());
                        }
                    }
                }

                // Унікалізація з збереженням порядку
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                desired = desired.Where(x => seen.Add(x)).ToList();

                if (desired.Count == 0)
                {
                    continue;
                }

                if (!existingByItemId.TryGetValue(entity.Id, out var current))
                {
                    current = new List<VocabularyItemTranslation>();
                    existingByItemId[entity.Id] = current;
                }

                // Видаляємо зайві та оновлюємо/додаємо потрібні, щоб Order йшов 1..N
                var currentByOrder = current.ToDictionary(x => x.Order, x => x);

                // remove translations that are not desired
                var remove = current.Where(x => !desired.Contains(x.Translation, StringComparer.OrdinalIgnoreCase)).ToList();
                if (remove.Count > 0)
                {
                    dbContext.VocabularyItemTranslations.RemoveRange(remove);
                }

                // ensure desired translations
                for (int i = 0; i < desired.Count; i++)
                {
                    var order = i + 1;
                    var tr = desired[i];

                    if (currentByOrder.TryGetValue(order, out var existingRow))
                    {
                        if (!string.Equals(existingRow.Translation, tr, StringComparison.OrdinalIgnoreCase))
                        {
                            // Якщо в цій же позиції інший текст — просто оновлюємо.
                            existingRow.Translation = tr;
                        }
                    }
                    else
                    {
                        dbContext.VocabularyItemTranslations.Add(new VocabularyItemTranslation
                        {
                            VocabularyItemId = entity.Id,
                            Translation = tr,
                            Order = order
                        });
                    }
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
                languageCode: "en",
                isPublished: true,
                level: "A1",
                order: 1,
                prerequisiteCourseId: null);

            // Additional courses for the Courses screen (A1 -> A2 -> B1 -> B2 -> C1)
            // These are explicit and stable: do not rely on Title parsing.
            var courseEnglishA2 = EnsureCourse(
                dbContext,
                title: "English A2",
                description: "Elementary: daily life, shopping, simple dialogs",
                languageCode: "en",
                isPublished: true,
                level: "A2",
                order: 2,
                prerequisiteCourseId: courseEnglish.Id);

            var courseEnglishB1 = EnsureCourse(
                dbContext,
                title: "English B1",
                description: "Intermediate: work, travel, opinions, longer texts",
                languageCode: "en",
                isPublished: true,
                level: "B1",
                order: 3,
                prerequisiteCourseId: courseEnglishA2.Id);

            var courseEnglishB2 = EnsureCourse(
                dbContext,
                title: "English B2",
                description: "Upper-intermediate: complex topics, fluency practice",
                languageCode: "en",
                isPublished: true,
                level: "B2",
                order: 4,
                prerequisiteCourseId: courseEnglishB1.Id);

            var courseEnglishC1 = EnsureCourse(
                dbContext,
                title: "English C1",
                description: "Advanced: academic and professional communication",
                languageCode: "en",
                isPublished: true,
                level: "C1",
                order: 5,
                prerequisiteCourseId: courseEnglishB2.Id);


            EnsurePrerequisitesForOrderedCourses(dbContext, languageCode: "en");
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

            EnsureFixedCourseStructureForDesign(dbContext, courseEnglish);
            EnsureFixedCourseStructureForDesign(dbContext, courseEnglishA2);
            EnsureFixedCourseStructureForDesign(dbContext, courseEnglishB1);
            EnsureFixedCourseStructureForDesign(dbContext, courseEnglishB2);
            EnsureFixedCourseStructureForDesign(dbContext, courseEnglishC1);
        }


        private static void EnsureFixedCourseStructureForDesign(LuminoDbContext dbContext, Course course)
        {
            if (course == null)
            {
                return;
            }

            // Design rule:
            // 1 course = 10 topics
            // 1 topic = 8 lessons + 1 final scene (sun)
            // 1 lesson = 9 exercises
            // This method is safe to call multiple times: it only adds missing items.

            var topicTitlesByOrder = new Dictionary<int, string>
            {
                [1] = "Buy clothes",
                [2] = "Food",
                [3] = "Greetings",
                [4] = "Numbers",
                [5] = "Travel",
                [6] = "Family",
                [7] = "Home",
                [8] = "Time",
                [9] = "Weather",
                [10] = "Hobbies"
            };

            var topics = dbContext.Topics
                .Where(x => x.CourseId == course.Id)
                .OrderBy(x => x.Order)
                .ToList();

            var byOrder = topics
                .GroupBy(x => x.Order)
                .ToDictionary(x => x.Key, x => x.First());

            for (int topicOrder = 1; topicOrder <= 10; topicOrder++)
            {
                if (byOrder.TryGetValue(topicOrder, out var fromDb))
                {
                    if (topicTitlesByOrder.TryGetValue(topicOrder, out var desiredTitle))
                    {
                        if (!string.Equals(fromDb.Title, desiredTitle, StringComparison.Ordinal))
                        {
                            fromDb.Title = desiredTitle;
                        }
                    }

                    continue;
                }

                var topic = new Topic
                {
                    CourseId = course.Id,
                    Title = topicTitlesByOrder.TryGetValue(topicOrder, out var title) ? title : $"Topic {topicOrder}",
                    Order = topicOrder
                };

                dbContext.Topics.Add(topic);
            }

            dbContext.SaveChanges();

            topics = dbContext.Topics
                .Where(x => x.CourseId == course.Id)
                .OrderBy(x => x.Order)
                .ToList();

            foreach (var topic in topics)
            {
                EnsureLessonsForTopic(dbContext, topic);
                EnsureFinalSceneForTopic(dbContext, course, topic);
            }

            dbContext.SaveChanges();
        }

        private static void EnsureLessonsForTopic(LuminoDbContext dbContext, Topic topic)
        {
            var lessons = dbContext.Lessons
                .Where(x => x.TopicId == topic.Id)
                .OrderBy(x => x.Order)
                .ToList();

            var lessonOrders = lessons
                .Select(x => x.Order)
                .Distinct()
                .ToHashSet();

            for (int lessonOrder = 1; lessonOrder <= 8; lessonOrder++)
            {
                if (lessonOrders.Contains(lessonOrder))
                {
                    continue;
                }

                var lesson = new Lesson
                {
                    TopicId = topic.Id,
                    Title = $"{topic.Title} - Lesson {lessonOrder}",
                    Theory = "TBD",
                    Order = lessonOrder
                };

                dbContext.Lessons.Add(lesson);
            }

            dbContext.SaveChanges();

            lessons = dbContext.Lessons
                .Where(x => x.TopicId == topic.Id)
                .OrderBy(x => x.Order)
                .ToList();

            foreach (var lesson in lessons)
            {
                EnsureExercisesForLesson(dbContext, lesson);
            }
        }

        private static void EnsureExercisesForLesson(LuminoDbContext dbContext, Lesson lesson)
        {
            var exercises = dbContext.Exercises
                .Where(x => x.LessonId == lesson.Id)
                .OrderBy(x => x.Order)
                .ToList();

            var exerciseOrders = exercises
                .Select(x => x.Order)
                .Distinct()
                .ToHashSet();

            var placeholderOptions = ToJsonStringArray("Option A", "Option B", "Option C");

            for (int exOrder = 1; exOrder <= 9; exOrder++)
            {
                if (exerciseOrders.Contains(exOrder))
                {
                    continue;
                }

                var exercise = new Exercise
                {
                    LessonId = lesson.Id,
                    Type = ExerciseType.MultipleChoice,
                    Question = $"Placeholder question {exOrder}",
                    Data = placeholderOptions,
                    CorrectAnswer = "Option A",
                    Order = exOrder
                };

                dbContext.Exercises.Add(exercise);
            }
        }

        private static void EnsureFinalSceneForTopic(LuminoDbContext dbContext, Course course, Topic topic)
        {
            // One final scene ("Sun") per topic. It is used by LearningPath rules (TopicId != null).
            // IMPORTANT: We check exactly Sun, not "any scene", to avoid duplicates like:
            // - linked dialog scene (not Sun) + additionally created Sun.
            var existingForTopic = dbContext.Scenes.FirstOrDefault(x => x.CourseId == course.Id && x.TopicId == topic.Id);
            if (existingForTopic != null)
            {
                var desiredOrder = 1000 + topic.Order;

                if (!string.Equals(existingForTopic.SceneType, "Sun", StringComparison.Ordinal))
                {
                    existingForTopic.SceneType = "Sun";
                }

                if (existingForTopic.Order != desiredOrder)
                {
                    existingForTopic.Order = desiredOrder;
                }

                if (string.IsNullOrWhiteSpace(existingForTopic.Title))
                {
                    existingForTopic.Title = $"{topic.Title} - Sun";
                }

                if (string.IsNullOrWhiteSpace(existingForTopic.Description))
                {
                    existingForTopic.Description = "Final topic scene (sun)";
                }

                return;
            }

            // If we already have dialog scenes seeded (SeedScenes), reuse them as topic "sun" scenes for the first topics.
            // This keeps your existing content (titles/steps) and only links it to the course/topic.
            var sceneTitlesByTopicOrder = new Dictionary<int, string>
            {
                [1] = "Cafe order",
                [2] = "Airport check-in",
                [3] = "Hotel booking",
                [4] = "Asking directions",
                [5] = "Shopping",
                [6] = "Small talk",
                [7] = "Restaurant reservation",
                [8] = "Doctor visit",
                [9] = "Public transport",
                [10] = "Job interview"
            };

            if (sceneTitlesByTopicOrder.TryGetValue(topic.Order, out var desiredTitle))
            {
                var fromDb = dbContext.Scenes.FirstOrDefault(x => x.Title == desiredTitle);
                if (fromDb != null)
                {
                    if (fromDb.CourseId != course.Id)
                    {
                        fromDb.CourseId = course.Id;
                    }

                    if (fromDb.TopicId != topic.Id)
                    {
                        fromDb.TopicId = topic.Id;
                    }

                    if (!string.Equals(fromDb.SceneType, "Sun", StringComparison.Ordinal))
                    {
                        fromDb.SceneType = "Sun";
                    }

                    // IMPORTANT: Scene.Order is unique per course (unique index CourseId + Order), so it must be unique across topics.
                    var desiredOrder = 1000 + topic.Order;
                    if (fromDb.Order != desiredOrder)
                    {
                        fromDb.Order = desiredOrder;
                    }

                    return;
                }
            }

            var scene = new Scene
            {
                CourseId = course.Id,
                TopicId = topic.Id,
                Title = $"{topic.Title} - Sun",
                Description = "Final topic scene (sun)",
                SceneType = "Sun",
                BackgroundUrl = null,
                AudioUrl = null,
                // IMPORTANT: Scene.Order is unique per course (unique index CourseId + Order),
                // so we must keep it unique across topics.
                Order = 1000 + topic.Order
            };

            dbContext.Scenes.Add(scene);
        }


        private static void SeedVocabularyLinks(LuminoDbContext dbContext)
        {
            SeedLessonVocabularyLinks(dbContext);
            SeedExerciseVocabularyLinks(dbContext);
        }

        private static void SeedLessonVocabularyLinks(LuminoDbContext dbContext)
        {
            var lessons = dbContext.Lessons.ToList();

            var vocabList = dbContext.VocabularyItems.ToList();
            var vocabMap = vocabList
                .GroupBy(x => NormalizeWord(x.Word))
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var lesson in lessons)
            {
                if (string.IsNullOrWhiteSpace(lesson.Theory))
                {
                    continue;
                }

                var lines = lesson.Theory
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                bool hasPairs = lines.Any(x => x.Contains('='));

                if (!hasPairs)
                {
                    var words = TheoryVocabularyExtractor.ExtractNonPairWords(lesson.Theory);

                    foreach (var w in words)
                    {
                        var key = NormalizeWord(w);

                        if (string.IsNullOrWhiteSpace(key))
                        {
                            continue;
                        }

                        if (!vocabMap.TryGetValue(key, out var existing))
                        {
                            continue;
                        }

                        var exists = dbContext.LessonVocabularies
                            .Any(x => x.LessonId == lesson.Id && x.VocabularyItemId == existing.Id);

                        if (exists)
                        {
                            continue;
                        }

                        dbContext.LessonVocabularies.Add(new LessonVocabulary
                        {
                            LessonId = lesson.Id,
                            VocabularyItemId = existing.Id
                        });
                    }

                    continue;
                }

                foreach (var line in lines)
                {
                    if (!line.Contains('='))
                    {
                        continue;
                    }

                    var parts = line.Split('=', 2);
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    var word = NormalizeWord(parts[0]);
                    var translation = NormalizeWord(parts[1]);

                    if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(translation))
                    {
                        continue;
                    }

                    var item = EnsureVocabularyItem(dbContext, vocabMap, word, translation);

                    var exists = dbContext.LessonVocabularies
                        .Any(x => x.LessonId == lesson.Id && x.VocabularyItemId == item.Id);

                    if (exists)
                    {
                        continue;
                    }

                    dbContext.LessonVocabularies.Add(new LessonVocabulary
                    {
                        LessonId = lesson.Id,
                        VocabularyItemId = item.Id
                    });
                }

            }

            dbContext.SaveChanges();
        }

        private static void SeedExerciseVocabularyLinks(LuminoDbContext dbContext)
        {
            var exercises = dbContext.Exercises.ToList();

            var vocabList = dbContext.VocabularyItems.ToList();
            var vocabMap = vocabList
                .GroupBy(x => NormalizeWord(x.Word))
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var exercise in exercises)
            {
                if (exercise.Type == ExerciseType.Match)
                {
                    SeedMatchExerciseVocabularyLinks(dbContext, vocabMap, exercise);
                    continue;
                }

                if (TryExtractPairFromQuestion(exercise.Question, exercise.CorrectAnswer, out var pair))
                {
                    UpsertExerciseVocabularyLink(dbContext, vocabMap, exercise.Id, pair.Word, pair.Translation);
                }
            }

            dbContext.SaveChanges();
        }

        private static void SeedMatchExerciseVocabularyLinks(
            LuminoDbContext dbContext,
            Dictionary<string, VocabularyItem> vocabMap,
            Exercise exercise)
        {
            if (string.IsNullOrWhiteSpace(exercise.Data))
            {
                return;
            }

            List<MatchPair>? pairs;

            try
            {
                pairs = JsonSerializer.Deserialize<List<MatchPair>>(exercise.Data);
            }
            catch
            {
                return;
            }

            if (pairs == null || pairs.Count == 0)
            {
                return;
            }

            foreach (var p in pairs)
            {
                var word = NormalizeWord(p.left);
                var translation = NormalizeWord(p.right);

                if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(translation))
                {
                    continue;
                }

                UpsertExerciseVocabularyLink(dbContext, vocabMap, exercise.Id, word, translation);
            }
        }

        private static void UpsertExerciseVocabularyLink(
            LuminoDbContext dbContext,
            Dictionary<string, VocabularyItem> vocabMap,
            int exerciseId,
            string word,
            string translation)
        {
            var item = EnsureVocabularyItem(dbContext, vocabMap, word, translation);

            var exists = dbContext.ExerciseVocabularies
                .Any(x => x.ExerciseId == exerciseId && x.VocabularyItemId == item.Id);

            if (exists)
            {
                return;
            }

            dbContext.ExerciseVocabularies.Add(new ExerciseVocabulary
            {
                ExerciseId = exerciseId,
                VocabularyItemId = item.Id
            });
        }

        private static VocabularyItem EnsureVocabularyItem(
            LuminoDbContext dbContext,
            Dictionary<string, VocabularyItem> vocabMap,
            string word,
            string translation)
        {
            var translations = SplitTranslations(translation);

            if (translations.Count == 0)
            {
                translations.Add(translation);
            }

            var primary = translations[0];

            if (!vocabMap.TryGetValue(word, out var item))
            {
                item = dbContext.VocabularyItems
                    .AsEnumerable()
                    .FirstOrDefault(x => NormalizeWord(x.Word) == word);

                if (item == null)
                {
                    item = new VocabularyItem
                    {
                        Word = word,
                        Translation = primary,
                        Example = null
                    };

                    dbContext.VocabularyItems.Add(item);
                    dbContext.SaveChanges();

                    EnsureVocabularyTranslation(dbContext, item.Id, primary, makePrimary: true);

                    for (var i = 1; i < translations.Count; i++)
                    {
                        EnsureVocabularyTranslation(dbContext, item.Id, translations[i], makePrimary: false);
                    }

                    vocabMap[word] = item;

                    return item;
                }

                vocabMap[word] = item;
            }

            foreach (var t in translations)
            {
                EnsureVocabularyTranslation(dbContext, item.Id, t, makePrimary: false);
            }

            return item;
        }


        private static void EnsureVocabularyTranslation(
            LuminoDbContext dbContext,
            int vocabularyItemId,
            string translation,
            bool makePrimary)
        {
            translation = (translation ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(translation))
            {
                return;
            }

            var normalizedTranslation = NormalizeWord(translation);

            var existing = dbContext.VocabularyItemTranslations
                .AsEnumerable()
                .FirstOrDefault(x => x.VocabularyItemId == vocabularyItemId
                    && NormalizeWord(x.Translation) == normalizedTranslation);

            if (existing != null)
            {
                if (makePrimary && existing.Order != 0)
                {
                    MakeTranslationPrimary(dbContext, vocabularyItemId, existing.Id, translation);
                }

                return;
            }

            var list = dbContext.VocabularyItemTranslations
                .Where(x => x.VocabularyItemId == vocabularyItemId)
                .ToList();

            // When we just started using translations table, existing VocabularyItem.Translation must stay primary.
            // So first ensure base translation (Order = 0) from legacy field, then add extra translations.
            if (!makePrimary && list.Count == 0)
            {
                var item = dbContext.VocabularyItems.FirstOrDefault(x => x.Id == vocabularyItemId);
                var baseTranslation = (item?.Translation ?? string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(baseTranslation))
                {
                    dbContext.VocabularyItemTranslations.Add(new VocabularyItemTranslation
                    {
                        VocabularyItemId = vocabularyItemId,
                        Translation = baseTranslation,
                        Order = 0
                    });

                    dbContext.SaveChanges();

                    // refresh list after insert
                    list = dbContext.VocabularyItemTranslations
                        .Where(x => x.VocabularyItemId == vocabularyItemId)
                        .ToList();

                    if (string.Equals(NormalizeWord(baseTranslation), normalizedTranslation, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            if (makePrimary)
            {
                foreach (var t in list)
                {
                    t.Order += 1;
                }

                dbContext.VocabularyItemTranslations.Add(new VocabularyItemTranslation
                {
                    VocabularyItemId = vocabularyItemId,
                    Translation = translation,
                    Order = 0
                });

                var item = dbContext.VocabularyItems.FirstOrDefault(x => x.Id == vocabularyItemId);
                if (item != null && item.Translation != translation)
                {
                    item.Translation = translation;
                }

                dbContext.SaveChanges();
                return;
            }

            var nextOrder = list.Count == 0 ? 0 : (list.Max(x => x.Order) + 1);

            dbContext.VocabularyItemTranslations.Add(new VocabularyItemTranslation
            {
                VocabularyItemId = vocabularyItemId,
                Translation = translation,
                Order = nextOrder
            });

            dbContext.SaveChanges();
        }

        private static void MakeTranslationPrimary(
            LuminoDbContext dbContext,
            int vocabularyItemId,
            int translationId,
            string translationText)
        {
            var list = dbContext.VocabularyItemTranslations
                .Where(x => x.VocabularyItemId == vocabularyItemId)
                .OrderBy(x => x.Order)
                .ToList();

            if (list.Count == 0)
            {
                return;
            }

            var target = list.FirstOrDefault(x => x.Id == translationId);
            if (target == null)
            {
                return;
            }

            foreach (var t in list)
            {
                if (t.Id == target.Id)
                {
                    continue;
                }

                if (t.Order < target.Order)
                {
                    t.Order += 1;
                }
            }

            target.Order = 0;

            var item = dbContext.VocabularyItems.FirstOrDefault(x => x.Id == vocabularyItemId);
            if (item != null && item.Translation != translationText)
            {
                item.Translation = translationText;
            }

            dbContext.SaveChanges();
        }

        private static bool TryExtractPairFromQuestion(string question, string correctAnswer, out (string Word, string Translation) pair)
        {
            pair = default;

            question = question ?? string.Empty;
            correctAnswer = correctAnswer ?? string.Empty;

            if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(correctAnswer))
            {
                return false;
            }

            var q = question.Trim();

            if (q.Contains("= ?"))
            {
                var beforeEq = q.Split('=')[0].Trim();

                var word = NormalizeWord(beforeEq);
                var translation = NormalizeWord(correctAnswer);

                if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(translation))
                {
                    return false;
                }

                pair = (word, translation);
                return true;
            }

            if (q.StartsWith("Write", StringComparison.OrdinalIgnoreCase) && q.Contains(':'))
            {
                var idx = q.IndexOf(':');
                var part = q.Substring(idx + 1).Trim();

                if (string.IsNullOrWhiteSpace(part))
                {
                    return false;
                }

                var isEnglish = q.Contains("Write English", StringComparison.OrdinalIgnoreCase);

                var word = isEnglish ? NormalizeWord(correctAnswer) : NormalizeWord(part);
                var translation = isEnglish ? NormalizeWord(part) : NormalizeWord(correctAnswer);

                if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(translation))
                {
                    return false;
                }

                pair = (word, translation);
                return true;
            }

            return false;
        }

        private static string NormalizeWord(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static List<string> SplitTranslations(string value)
        {
            value = (value ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            // Support simple multi-translation formats:
            // "bank = банк / берег", "bank = банк, берег", "bank = банк; берег"
            // Keep order, remove duplicates (case-insensitive).
            value = value
                .Replace(" або ", "|", StringComparison.OrdinalIgnoreCase)
                .Replace(" or ", "|", StringComparison.OrdinalIgnoreCase);

            var parts = value
                .Split(new[] { '/', ',', ';', '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var result = new List<string>();

            foreach (var p in parts)
            {
                var normalized = NormalizeWord(p);

                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (!result.Any(x => string.Equals(NormalizeWord(x), normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(p);
                }
            }

            return result;
        }

        private static void EnsurePrerequisitesForOrderedCourses(LuminoDbContext dbContext, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return;
            }

            var normalized = languageCode.Trim().ToLowerInvariant();

            var courses = dbContext.Courses
                .Where(x => x.LanguageCode == normalized && x.Order > 0)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            if (courses.Count < 2)
            {
                return;
            }

            var changed = false;
            Course? previous = null;

            foreach (var c in courses)
            {
                if (previous == null)
                {
                    previous = c;
                    continue;
                }

                if (c.PrerequisiteCourseId == null)
                {
                    c.PrerequisiteCourseId = previous.Id;
                    changed = true;
                }

                previous = c;
            }

            if (changed)
            {
                dbContext.SaveChanges();
            }
        }


        private static Course EnsureCourse(LuminoDbContext dbContext, string title, string description, string languageCode, bool isPublished, string? level, int order, int? prerequisiteCourseId)
        {
            var fromDb = dbContext.Courses.FirstOrDefault(x => x.Title == title);

            if (fromDb == null)
            {
                var course = new Course
                {
                    Title = title,
                    Description = description,
                    LanguageCode = (languageCode ?? "en").Trim().ToLowerInvariant(),
                    Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim().ToUpperInvariant(),
                    Order = order,
                    PrerequisiteCourseId = prerequisiteCourseId,
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

            var normalizedLanguageCode = (languageCode ?? "en").Trim().ToLowerInvariant();

            if (fromDb.LanguageCode != normalizedLanguageCode)
            {
                fromDb.LanguageCode = normalizedLanguageCode;
                changed = true;
            }

            var normalizedLevel = string.IsNullOrWhiteSpace(level) ? null : level.Trim().ToUpperInvariant();

            if (fromDb.Level != normalizedLevel)
            {
                fromDb.Level = normalizedLevel;
                changed = true;
            }

            if (fromDb.Order != order)
            {
                fromDb.Order = order;
                changed = true;
            }

            if (fromDb.PrerequisiteCourseId != prerequisiteCourseId)
            {
                fromDb.PrerequisiteCourseId = prerequisiteCourseId;
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


        private static void EnsureStrictCourseStructure(LuminoDbContext dbContext, int courseId)
        {
            // This is our content pipeline rule for UI: 1 course = 10 topics, 1 topic = 8 lessons, 1 lesson = 9 exercises.
            // Seeder makes it true for the default demo course too.
            var topics = dbContext.Topics
                .Where(x => x.CourseId == courseId)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            if (topics.Count < 10)
            {
                var startOrder = topics.Count == 0 ? 1 : topics.Max(x => x.Order) + 1;

                for (int i = topics.Count + 1; i <= 10; i++)
                {
                    dbContext.Topics.Add(new Topic
                    {
                        CourseId = courseId,
                        Title = $"Topic {i}",
                        Order = startOrder++
                    });
                }

                dbContext.SaveChanges();

                topics = dbContext.Topics
                    .Where(x => x.CourseId == courseId)
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .ToList();
            }

            foreach (var topic in topics.Take(10))
            {
                EnsureLessonsForTopic(dbContext, topic.Id, topic.Order);
            }
        }

        private static void EnsureLessonsForTopic(LuminoDbContext dbContext, int topicId, int topicOrder)
        {
            var lessons = dbContext.Lessons
                .Where(x => x.TopicId == topicId)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            if (lessons.Count < 8)
            {
                var startOrder = lessons.Count == 0 ? 1 : lessons.Max(x => x.Order) + 1;

                for (int i = lessons.Count + 1; i <= 8; i++)
                {
                    dbContext.Lessons.Add(new Lesson
                    {
                        TopicId = topicId,
                        Title = $"Topic {topicOrder} — Lesson {i}",
                        Theory = "Coming soon",
                        Order = startOrder++
                    });
                }

                dbContext.SaveChanges();

                lessons = dbContext.Lessons
                    .Where(x => x.TopicId == topicId)
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id)
                    .ToList();
            }

            foreach (var lesson in lessons.Take(8))
            {
                EnsureExercisesForLesson(dbContext, lesson.Id);
            }
        }

        private static void EnsureExercisesForLesson(LuminoDbContext dbContext, int lessonId)
        {
            var exercises = dbContext.Exercises
                .Where(x => x.LessonId == lessonId)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            if (exercises.Count >= 9)
            {
                return;
            }

            var startOrder = exercises.Count == 0 ? 1 : exercises.Max(x => x.Order) + 1;

            for (int i = exercises.Count + 1; i <= 9; i++)
            {
                dbContext.Exercises.Add(new Exercise
                {
                    LessonId = lessonId,
                    Type = ExerciseType.MultipleChoice,
                    Question = $"Demo question {i}",
                    Data = ToJsonStringArray("Option A", "Option B", "Option C"),
                    CorrectAnswer = "Option A",
                    Order = startOrder++
                });
            }

            dbContext.SaveChanges();
        }

        private record TopicSeed(string Title, int Order);

        private record LessonSeed(int TopicId, string Title, string Theory, int Order);

        private record ExerciseSeed(ExerciseType Type, string Question, string Data, string CorrectAnswer, int Order);
        private static void EnsureVocabularyBaseTranslations(LuminoDbContext dbContext)
        {
            var items = dbContext.VocabularyItems
                .AsNoTracking()
                .Select(x => new { x.Id, x.Translation })
                .ToList();

            if (items.Count == 0)
            {
                return;
            }

            var translations = dbContext.VocabularyItemTranslations
                .ToList();

            var byItem = translations
                .GroupBy(x => x.VocabularyItemId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var hasChanges = false;

            foreach (var vi in items)
            {
                if (!byItem.TryGetValue(vi.Id, out var list) || list.Count == 0)
                {
                    dbContext.VocabularyItemTranslations.Add(new VocabularyItemTranslation
                    {
                        VocabularyItemId = vi.Id,
                        Translation = vi.Translation,
                        Order = 0
                    });

                    hasChanges = true;
                    continue;
                }

                var primary = list.OrderBy(x => x.Order).FirstOrDefault(x => x.Order == 0);
                if (primary == null)
                {
                    var minOrder = list.Min(x => x.Order);
                    primary = list.First(x => x.Order == minOrder);
                    primary.Order = 0;
                    hasChanges = true;
                }

                if (!string.Equals(primary.Translation, vi.Translation, StringComparison.Ordinal))
                {
                    primary.Translation = vi.Translation;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                dbContext.SaveChanges();
            }
        }

    }
}
