using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class DesignScenariosHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public DesignScenariosHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Home_PassLesson_ShouldIncreaseStreak_AndCalendarMonthShouldMatchMacet()
    {
        var today = DateTime.UtcNow.Date;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "test@test.com",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = DateTime.UtcNow
            });

            dbContext.Courses.Add(new Course
            {
                Id = 1,
                Title = "English A1",
                Description = "Desc",
                IsPublished = true,
                LanguageCode = "en"
            });

            dbContext.Topics.Add(new Topic
            {
                Id = 1,
                CourseId = 1,
                Title = "Topic",
                Order = 1
            });

            dbContext.Lessons.Add(new Lesson
            {
                Id = 1,
                TopicId = 1,
                Title = "L1",
                Theory = "",
                Order = 1
            });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 1,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q1",
                Data = "",
                CorrectAnswer = "a",
                Order = 1
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var meBeforeResponse = await client.GetAsync("/api/user/me");
        Assert.Equal(HttpStatusCode.OK, meBeforeResponse.StatusCode);

        var meBeforeJson = await meBeforeResponse.Content.ReadAsStringAsync();
        using (var meBeforeDoc = JsonDocument.Parse(meBeforeJson))
        {
            AssertJsonHasProperties(meBeforeDoc.RootElement, "id", "email", "hearts", "currentStreakDays", "bestStreakDays");
            Assert.Equal(0, GetInt32PropertyIgnoreCase(meBeforeDoc.RootElement, "currentStreakDays"));
        }

        var submitLessonRequest = new
        {
            lessonId = 1,
            idempotencyKey = "lesson-streak-key-1",
            answers = new[]
            {
                new { exerciseId = 1, answer = "a" }
            }
        };

        var submitLessonJson = JsonSerializer.Serialize(submitLessonRequest);
        var submitLessonResponse = await client.PostAsync("/api/lesson-submit", new StringContent(submitLessonJson, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, submitLessonResponse.StatusCode);

        var meAfterResponse = await client.GetAsync("/api/user/me");
        Assert.Equal(HttpStatusCode.OK, meAfterResponse.StatusCode);

        var meAfterJson = await meAfterResponse.Content.ReadAsStringAsync();
        using (var meAfterDoc = JsonDocument.Parse(meAfterJson))
        {
            Assert.Equal(1, GetInt32PropertyIgnoreCase(meAfterDoc.RootElement, "currentStreakDays"));
            Assert.True(GetInt32PropertyIgnoreCase(meAfterDoc.RootElement, "bestStreakDays") >= 1);
        }

        var streakResponse = await client.GetAsync("/api/streak/me");
        Assert.Equal(HttpStatusCode.OK, streakResponse.StatusCode);

        var streakJson = await streakResponse.Content.ReadAsStringAsync();
        using (var streakDoc = JsonDocument.Parse(streakJson))
        {
            AssertJsonHasProperties(streakDoc.RootElement, "current", "best");
            Assert.Equal(1, GetInt32PropertyIgnoreCase(streakDoc.RootElement, "current"));
        }

        var calendarResponse = await client.GetAsync($"/api/streak/calendar?year={today.Year}&month={today.Month}");
        Assert.Equal(HttpStatusCode.OK, calendarResponse.StatusCode);

        var calendarJson = await calendarResponse.Content.ReadAsStringAsync();
        using (var calendarDoc = JsonDocument.Parse(calendarJson))
        {
            AssertJsonHasProperties(calendarDoc.RootElement, "year", "month", "days");

            Assert.Equal(today.Year, GetInt32PropertyIgnoreCase(calendarDoc.RootElement, "year"));
            Assert.Equal(today.Month, GetInt32PropertyIgnoreCase(calendarDoc.RootElement, "month"));

            var days = GetPropertyIgnoreCase(calendarDoc.RootElement, "days");
            Assert.Equal(JsonValueKind.Array, days.ValueKind);

            var expectedDaysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            Assert.Equal(expectedDaysInMonth, days.GetArrayLength());

            var anyActiveToday = days.EnumerateArray().Any(x =>
                TryGetPropertyIgnoreCase(x, "dateUtc", out var d) &&
                DateTime.Parse(d.GetString()!).Date == today &&
                TryGetPropertyIgnoreCase(x, "isActive", out var a) && a.GetBoolean());

            Assert.True(anyActiveToday);
        }
    }

    [Fact]
    public async Task Dictionary_ItemDetails_ShouldContainTranscriptionAndGender()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "test@test.com",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = DateTime.UtcNow
            });

            dbContext.VocabularyItems.Add(new VocabularyItem
            {
                Id = 100,
                Word = "der Hund",
                Translation = "собака",
                PartOfSpeech = "noun",
                Definition = "a domestic animal",
                Transcription = "[hʊnt]",
                Gender = "der",
                ExamplesJson = "[{\"text\":\"Der Hund ist groß.\",\"translation\":\"Собака велика.\"}]",
                SynonymsJson = "[{\"text\":\"Tier\",\"translation\":\"тварина\"}]",
                IdiomsJson = "[{\"text\":\"auf den Hund kommen\",\"translation\":\"злиденніти\"}]"
            });

            dbContext.UserVocabularies.Add(new UserVocabulary
            {
                UserId = 10,
                VocabularyItemId = 100,
                AddedAt = DateTime.UtcNow,
                NextReviewAt = DateTime.UtcNow.AddDays(1)
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/vocabulary/items/100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        AssertJsonHasProperties(doc.RootElement, "id", "word", "translation", "partOfSpeech", "definition", "transcription", "gender", "examples", "synonyms", "idioms");
        Assert.Equal("[hʊnt]", GetStringPropertyIgnoreCase(doc.RootElement, "transcription"));
        Assert.Equal("der", GetStringPropertyIgnoreCase(doc.RootElement, "gender"));
    }

    [Fact]
    public async Task Profile_WeeklyProgress_ShouldReturnCurrentAndPreviousWeeksAndTotalPoints()
    {
        var today = DateTime.UtcNow.Date;
        int offset = ((int)today.DayOfWeek + 6) % 7;
        var currentWeekStart = today.AddDays(-offset);
        var previousWeekStart = currentWeekStart.AddDays(-7);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "test@test.com",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = DateTime.UtcNow
            });

            dbContext.LessonResults.AddRange(
                new LessonResult { UserId = 10, LessonId = 1, Score = 10, TotalQuestions = 10, CompletedAt = currentWeekStart.AddDays(0) },
                new LessonResult { UserId = 10, LessonId = 2, Score = 15, TotalQuestions = 10, CompletedAt = currentWeekStart.AddDays(2) },
                new LessonResult { UserId = 10, LessonId = 3, Score = 7, TotalQuestions = 10, CompletedAt = previousWeekStart.AddDays(1) }
            );

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/profile/weekly-progress");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        AssertJsonHasProperties(doc.RootElement, "totalPoints", "currentWeek", "previousWeek");
        Assert.Equal(32, GetInt32PropertyIgnoreCase(doc.RootElement, "totalPoints"));
        Assert.Equal(7, GetPropertyIgnoreCase(doc.RootElement, "currentWeek").GetArrayLength());
        Assert.Equal(7, GetPropertyIgnoreCase(doc.RootElement, "previousWeek").GetArrayLength());
    }

    [Fact]
    public async Task Courses_Me_ShouldLockNextCourseUntilPreviousCompleted()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "test@test.com",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = DateTime.UtcNow
            });

            dbContext.Courses.AddRange(
                new Course { Id = 1, Title = "English A1", Description = "Desc", IsPublished = true, LanguageCode = "en" },
                new Course { Id = 2, Title = "English A2", Description = "Desc", IsPublished = true, LanguageCode = "en" }
            );

            dbContext.Topics.AddRange(
                new Topic { Id = 1, CourseId = 1, Title = "T1", Order = 1 },
                new Topic { Id = 2, CourseId = 2, Title = "T2", Order = 1 }
            );

            dbContext.Lessons.AddRange(
                new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "", Order = 1 },
                new Lesson { Id = 2, TopicId = 2, Title = "L2", Theory = "", Order = 1 }
            );

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses/me?languageCode=en");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        var list = doc.RootElement.EnumerateArray().ToList();
        Assert.True(list.Count >= 2);

        var a1 = list.FirstOrDefault(x => string.Equals(GetStringPropertyIgnoreCase(x, "level"), "A1", StringComparison.OrdinalIgnoreCase));
        var a2 = list.FirstOrDefault(x => string.Equals(GetStringPropertyIgnoreCase(x, "level"), "A2", StringComparison.OrdinalIgnoreCase));

        Assert.True(a1.ValueKind == JsonValueKind.Object);
        Assert.True(a2.ValueKind == JsonValueKind.Object);

        Assert.False(GetBoolPropertyIgnoreCase(a1, "isLocked"));
        Assert.True(GetBoolPropertyIgnoreCase(a2, "isLocked"));
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var p in element.EnumerateObject())
        {
            if (string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }

        return false;
    }

    private static JsonElement GetPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            return value;
        }

        Assert.Fail("Missing JSON property: " + propertyName);
        return default;
    }

    private static int GetInt32PropertyIgnoreCase(JsonElement element, string propertyName)
    {
        return GetPropertyIgnoreCase(element, propertyName).GetInt32();
    }

    private static bool GetBoolPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        return GetPropertyIgnoreCase(element, propertyName).GetBoolean();
    }

    private static string GetStringPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        return GetPropertyIgnoreCase(element, propertyName).GetString() ?? string.Empty;
    }

    private static void AssertJsonHasProperties(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            Assert.True(TryGetPropertyIgnoreCase(element, propertyName, out _), "Missing JSON property: " + propertyName);
        }
    }
}
