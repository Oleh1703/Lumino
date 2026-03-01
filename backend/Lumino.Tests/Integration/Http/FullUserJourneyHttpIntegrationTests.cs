using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class FullUserJourneyHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public FullUserJourneyHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullJourney_HappyPath_ShouldMatchMainScreensContracts()
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

            // Courses: explicit Level/Order/Prerequisite (залізобетон, без Title-залежності)
            dbContext.Courses.Add(new Course
            {
                Id = 1,
                Title = "English A1",
                Description = "Desc",
                IsPublished = true,
                LanguageCode = "en",
                Level = "A1",
                Order = 1,
                PrerequisiteCourseId = null
            });

            dbContext.Courses.Add(new Course
            {
                Id = 2,
                Title = "English A2",
                Description = "Desc",
                IsPublished = true,
                LanguageCode = "en",
                Level = "A2",
                Order = 2,
                PrerequisiteCourseId = 1
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

            // Minimal Scene for "Scenes archive" screen
            dbContext.Scenes.Add(new Scene
            {
                Id = 1,
                CourseId = 1,
                TopicId = 1,
                Order = 1,
                Title = "Scene 1",
                Description = "Scene desc",
                SceneType = "dialog"
            });

            dbContext.SceneSteps.Add(new SceneStep
            {
                Id = 1,
                SceneId = 1,
                Order = 1,
                Speaker = "Narrator",
                Text = "Hello",
                StepType = "text"
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        // 1) Profile (hearts/streak contracts)
        var meResponse = await client.GetAsync("/api/user/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var meJson = await meResponse.Content.ReadAsStringAsync();
        using (var meDoc = JsonDocument.Parse(meJson))
        {
            AssertJsonHasProperties(meDoc.RootElement,
                "id", "email",
                "hearts", "crystals",
                "heartsMax", "heartRegenMinutes", "crystalCostPerHeart",
                "nextHeartAtUtc", "nextHeartInSeconds",
                "currentStreakDays", "bestStreakDays"
            );
        }

        // 2) Courses (A2 must be locked until A1 completed)
        var coursesResponse = await client.GetAsync("/api/courses/me");
        Assert.Equal(HttpStatusCode.OK, coursesResponse.StatusCode);

        var coursesJson = await coursesResponse.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(coursesJson))
        {
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

        // 3) Start learning course (home screen action)
        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        // 4) Submit lesson (learning flow -> streak should increase)
        var submitLessonRequest = new
        {
            lessonId = 1,
            idempotencyKey = "full-journey-key-1",
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
            Assert.True(GetInt32PropertyIgnoreCase(meAfterDoc.RootElement, "currentStreakDays") >= 1);
            Assert.True(GetInt32PropertyIgnoreCase(meAfterDoc.RootElement, "bestStreakDays") >= 1);
        }

        // 5) Streak + calendar (profile)
        var streakResponse = await client.GetAsync("/api/streak/me");
        Assert.Equal(HttpStatusCode.OK, streakResponse.StatusCode);

        var calendarResponse = await client.GetAsync($"/api/streak/calendar?year={today.Year}&month={today.Month}");
        Assert.Equal(HttpStatusCode.OK, calendarResponse.StatusCode);

        // 6) Scenes archive (scenes screen)
        var scenesResponse = await client.GetAsync("/api/scenes/me?courseId=1");
        Assert.Equal(HttpStatusCode.OK, scenesResponse.StatusCode);

        var scenesJson = await scenesResponse.Content.ReadAsStringAsync();
        using (var scenesDoc = JsonDocument.Parse(scenesJson))
        {
            Assert.Equal(JsonValueKind.Array, scenesDoc.RootElement.ValueKind);
            var arr = scenesDoc.RootElement.EnumerateArray().ToList();
            Assert.True(arr.Count >= 1);

            // Contract sanity: scene details should at least have id/title/order and lock flags
            var first = arr[0];
            AssertJsonHasProperties(first, "id", "title", "order", "isUnlocked", "isCompleted");
        }

        // 7) Vocabulary screen should respond (even if empty)
        var vocabResponse = await client.GetAsync("/api/vocabulary/me");
        Assert.Equal(HttpStatusCode.OK, vocabResponse.StatusCode);
    }

    private static void AssertJsonHasProperties(JsonElement element, params string[] properties)
    {
        foreach (var p in properties)
        {
            Assert.True(TryGetPropertyIgnoreCase(element, p, out _), $"Missing property '{p}'");
        }
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

    private static string? GetStringPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    private static bool GetBoolPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.True) return true;
        if (value.ValueKind == JsonValueKind.False) return false;

        return false;
    }

    private static int GetInt32PropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
        {
            return i;
        }

        return 0;
    }
}
