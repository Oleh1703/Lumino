using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class ProblemDetailsHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ProblemDetailsHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LockedLesson_ShouldReturnForbidden_InProblemDetailsFormat()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SeedBaseUserAndCourse(dbContext);
            SeedLessonWithExercises(dbContext, lessonId: 1, topicId: 1, order: 1);
            SeedLessonWithExercises(dbContext, lessonId: 2, topicId: 1, order: 2);

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        // start course -> unlock тільки перший урок
        var startCourse = await client.PostAsync("/api/learning/courses/1/start", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, startCourse.StatusCode);

        var response = await client.GetAsync("/api/lessons/2");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(body))
        {
            var root = doc.RootElement;

            AssertJsonHasProperties(root, "type", "title", "status", "detail", "instance", "traceId");

            Assert.Equal("forbidden", root.GetProperty("type").GetString());
            Assert.Equal(403, root.GetProperty("status").GetInt32());

            var detail = root.GetProperty("detail").GetString() ?? "";
            Assert.Contains("Lesson is locked", detail);
        }
    }

    [Fact]
    public async Task InvalidSubmit_ShouldReturnBadRequest_InProblemDetailsFormat()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        var client = _factory.CreateClient();

        var payload = new
        {
            lessonId = 0,
            answers = new[] { new { exerciseId = 1, answer = "a" } }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/lesson-submit", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(body))
        {
            var root = doc.RootElement;

            AssertJsonHasProperties(root, "type", "title", "status", "detail", "instance", "traceId");

            Assert.Equal("bad_request", root.GetProperty("type").GetString());
            Assert.Equal(400, root.GetProperty("status").GetInt32());

            var detail = root.GetProperty("detail").GetString() ?? "";
            Assert.Contains("LessonId is invalid", detail);
        }
    }

    private static void SeedBaseUserAndCourse(LuminoDbContext dbContext)
    {
        dbContext.Users.Add(new User
        {
            Id = 10,
            Email = "test@test.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Topic",
            Order = 1
        });
    }

    private static void SeedLessonWithExercises(LuminoDbContext dbContext, int lessonId, int topicId, int order)
    {
        dbContext.Lessons.Add(new Lesson
        {
            Id = lessonId,
            TopicId = topicId,
            Title = "Lesson " + lessonId,
            Theory = "",
            Order = order
        });

        dbContext.Exercises.AddRange(
            new Exercise
            {
                Id = (lessonId * 100) + 1,
                LessonId = lessonId,
                Type = ExerciseType.Input,
                Question = "Q1",
                Data = "",
                CorrectAnswer = "a",
                Order = 1
            },
            new Exercise
            {
                Id = (lessonId * 100) + 2,
                LessonId = lessonId,
                Type = ExerciseType.Input,
                Question = "Q2",
                Data = "",
                CorrectAnswer = "a",
                Order = 2
            }
        );
    }

    private static void AssertJsonHasProperties(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            Assert.True(element.TryGetProperty(propertyName, out _), "Missing JSON property: " + propertyName);
        }
    }
}
