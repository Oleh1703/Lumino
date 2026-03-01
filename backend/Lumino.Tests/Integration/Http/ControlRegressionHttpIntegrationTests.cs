using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class ControlRegressionHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ControlRegressionHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Courses_Me_ShouldLockNextCourse_WhenPrerequisiteIsNull_ButOrderIsSet()
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
                PrerequisiteCourseId = null
            });

            // Course 1 has at least one lesson so CourseCompletionService considers userCourse state
            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "T1", Order = 1 });
            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "", Order = 1 });

            dbContext.UserCourses.Add(new UserCourse
            {
                UserId = 10,
                CourseId = 1,
                IsActive = true,
                IsCompleted = false,
                StartedAt = DateTime.UtcNow,
                LastOpenedAt = DateTime.UtcNow
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.ValueKind == JsonValueKind.Array);

        var a2 = FindCourseById(doc.RootElement, 2);

        AssertJsonHasProperties(a2, "id", "isLocked", "order");

        // A2 has no explicit prerequisite, but Order is set and A1 is not completed -> should be locked (inferred prerequisite).
        Assert.True(GetBooleanPropertyIgnoreCase(a2, "isLocked"));
    }

    [Fact]
    public async Task Courses_Me_ShouldPreferExplicitPrerequisite_OverInferredByOrder()
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
                PrerequisiteCourseId = null
            });

            dbContext.Courses.Add(new Course
            {
                Id = 3,
                Title = "English B1",
                Description = "Desc",
                IsPublished = true,
                LanguageCode = "en",
                Level = "B1",
                Order = 3,

                // Explicit prerequisite: A1 (id=1), even though inferred by order would be A2 (id=2).
                PrerequisiteCourseId = 1
            });

            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "T1", Order = 1 });
            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "", Order = 1 });

            dbContext.UserCourses.Add(new UserCourse
            {
                UserId = 10,
                CourseId = 1,
                IsActive = false,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow.AddDays(-2),
                LastOpenedAt = DateTime.UtcNow.AddDays(-1)
            });

            dbContext.UserCourses.Add(new UserCourse
            {
                UserId = 10,
                CourseId = 2,
                IsActive = true,
                IsCompleted = false,
                StartedAt = DateTime.UtcNow,
                LastOpenedAt = DateTime.UtcNow
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var b1 = FindCourseById(doc.RootElement, 3);

        AssertJsonHasProperties(b1, "id", "isLocked", "prerequisiteCourseId");

        // Because explicit prerequisite A1 is completed, B1 must be unlocked even if A2 is not completed.
        Assert.False(GetBooleanPropertyIgnoreCase(b1, "isLocked"));
    }

    [Fact]
    public async Task Hearts_EconomyFields_ShouldBeConsistent_BetweenGetMe_AndRestoreHearts()
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
                CreatedAt = DateTime.UtcNow,

                Hearts = 3,
                HeartsUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),

                Crystals = 999
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        // GetMe snapshot
        var meResponse = await client.GetAsync("/api/user/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var meJson = await meResponse.Content.ReadAsStringAsync();
        using var meDoc = JsonDocument.Parse(meJson);

        AssertJsonHasProperties(meDoc.RootElement,
            "hearts", "crystals",
            "heartsMax", "heartRegenMinutes", "crystalCostPerHeart",
            "nextHeartAtUtc", "nextHeartInSeconds");

        var heartsMax1 = GetInt32PropertyIgnoreCase(meDoc.RootElement, "heartsMax");
        var regen1 = GetInt32PropertyIgnoreCase(meDoc.RootElement, "heartRegenMinutes");
        var cost1 = GetInt32PropertyIgnoreCase(meDoc.RootElement, "crystalCostPerHeart");

        // Restore 0 hearts: must still return consistent economy metadata (even if restoredHearts=0).
        var restoreBody = JsonSerializer.Serialize(new { heartsToRestore = 0 });
        var restoreResponse = await client.PostAsync("/api/user/restore-hearts", new StringContent(restoreBody, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        var restoreJson = await restoreResponse.Content.ReadAsStringAsync();
        using var restoreDoc = JsonDocument.Parse(restoreJson);

        AssertJsonHasProperties(restoreDoc.RootElement,
            "hearts", "crystals",
            "heartsMax", "heartRegenMinutes", "crystalCostPerHeart",
            "nextHeartAtUtc", "nextHeartInSeconds",
            "spentCrystals", "restoredHearts");

        var heartsMax2 = GetInt32PropertyIgnoreCase(restoreDoc.RootElement, "heartsMax");
        var regen2 = GetInt32PropertyIgnoreCase(restoreDoc.RootElement, "heartRegenMinutes");
        var cost2 = GetInt32PropertyIgnoreCase(restoreDoc.RootElement, "crystalCostPerHeart");

        Assert.Equal(heartsMax1, heartsMax2);
        Assert.Equal(regen1, regen2);
        Assert.Equal(cost1, cost2);
    }

    private static JsonElement FindCourseById(JsonElement root, int id)
    {
        foreach (var item in root.EnumerateArray())
        {
            if (GetInt32PropertyIgnoreCase(item, "id") == id)
            {
                return item;
            }
        }

        throw new InvalidOperationException("Course not found in response");
    }

    private static void AssertJsonHasProperties(JsonElement element, params string[] propertyNames)
    {
        for (int i = 0; i < propertyNames.Length; i++)
        {
            var name = propertyNames[i];

            if (!HasPropertyIgnoreCase(element, name))
            {
                throw new Xunit.Sdk.XunitException("JSON is missing required property: " + name);
            }
        }
    }

    private static bool HasPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int GetInt32PropertyIgnoreCase(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return prop.Value.GetInt32();
            }
        }

        throw new InvalidOperationException("Property not found: " + propertyName);
    }

    private static bool GetBooleanPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return prop.Value.GetBoolean();
            }
        }

        throw new InvalidOperationException("Property not found: " + propertyName);
    }
}
