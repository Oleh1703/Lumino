using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class MistakesFlowHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public MistakesFlowHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartCourse_NextLesson_FailThenMistakesPass_ShouldUnlockNextLesson_AndNextShouldReturnLesson2()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course
            {
                Id = 1,
                Title = "Course",
                Description = "Desc",
                IsPublished = true
            });

            // Topic 1 / Lesson 1
            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "Topic 1", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "Lesson 1", Theory = "", Order = 1 });

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

            dbContext.Exercises.Add(new Exercise
            {
                Id = 2,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q2",
                Data = "",
                CorrectAnswer = "b",
                Order = 2
            });

            // Topic 2 / Lesson 2 (locked until Lesson 1 passed)
            dbContext.Topics.Add(new Topic { Id = 2, CourseId = 1, Title = "Topic 2", Order = 2 });

            dbContext.Lessons.Add(new Lesson { Id = 2, TopicId = 2, Title = "Lesson 2", Theory = "", Order = 1 });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 3,
                LessonId = 2,
                Type = ExerciseType.Input,
                Question = "Q3",
                Data = "",
                CorrectAnswer = "c",
                Order = 1
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        // next -> Lesson 1
        var next1 = await client.GetAsync("/api/next/me");

        Assert.Equal(HttpStatusCode.OK, next1.StatusCode);

        var next1Json = await next1.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(next1Json))
        {
            Assert.Equal("Lesson", doc.RootElement.GetProperty("type").GetString());
            Assert.Equal(1, doc.RootElement.GetProperty("lessonId").GetInt32());
            Assert.Equal(1, doc.RootElement.GetProperty("courseId").GetInt32());
            Assert.False(doc.RootElement.GetProperty("isLocked").GetBoolean());
            Assert.True(doc.RootElement.TryGetProperty("topicId", out _));
            Assert.True(doc.RootElement.TryGetProperty("lessonTitle", out _));
        }

        // submit lesson with 1 correct and 1 wrong -> fail
        var submitPayload = new
        {
            lessonId = 1,
            idempotencyKey = "k-l1-attempt-1",
            answers = new[]
            {
                new { exerciseId = 1, answer = "a" },
                new { exerciseId = 2, answer = "WRONG" }
            }
        };

        var submitJson = JsonSerializer.Serialize(submitPayload);

        var submitResponse = await client.PostAsync(
            "/api/lesson-submit",
            new StringContent(submitJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var submitBody = await submitResponse.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(submitBody))
        {
            Assert.False(doc.RootElement.GetProperty("isPassed").GetBoolean());

            var mistakeIds = doc.RootElement.GetProperty("mistakeExerciseIds");
            Assert.Equal(1, mistakeIds.GetArrayLength());
            Assert.Equal(2, mistakeIds[0].GetInt32());

            // DTO contract: answers always present (even if some are correct)
            Assert.True(doc.RootElement.TryGetProperty("answers", out var answers));
            Assert.True(answers.ValueKind == JsonValueKind.Array);
            Assert.Equal(2, answers.GetArrayLength());
        }

        // get mistakes
        var mistakesResponse = await client.GetAsync("/api/lessons/1/mistakes");

        Assert.Equal(HttpStatusCode.OK, mistakesResponse.StatusCode);

        var mistakesBody = await mistakesResponse.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(mistakesBody))
        {
            Assert.Equal(1, doc.RootElement.GetProperty("lessonId").GetInt32());
            Assert.Equal(1, doc.RootElement.GetProperty("totalMistakes").GetInt32());

            var mistakeIds = doc.RootElement.GetProperty("mistakeExerciseIds");
            Assert.Equal(1, mistakeIds.GetArrayLength());
            Assert.Equal(2, mistakeIds[0].GetInt32());

            var exercises = doc.RootElement.GetProperty("exercises");
            Assert.Equal(1, exercises.GetArrayLength());

            var ex = exercises[0];

            // DTO contract check (fields must exist for frontend)
            Assert.Equal(2, ex.GetProperty("id").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(ex.GetProperty("type").GetString()));
            Assert.True(ex.TryGetProperty("question", out _));
            Assert.True(ex.TryGetProperty("data", out _));
            Assert.True(ex.TryGetProperty("order", out _));
        }

        // submit mistakes with correct answer for exercise 2 -> pass
        var mistakesSubmitPayload = new
        {
            answers = new[]
            {
                new { exerciseId = 2, answer = "b" }
            }
        };

        var mistakesSubmitJson = JsonSerializer.Serialize(mistakesSubmitPayload);

        var mistakesSubmitResponse = await client.PostAsync(
            "/api/lessons/1/mistakes/submit",
            new StringContent(mistakesSubmitJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, mistakesSubmitResponse.StatusCode);

        var mistakesSubmitBody = await mistakesSubmitResponse.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(mistakesSubmitBody))
        {
            Assert.Equal(1, doc.RootElement.GetProperty("lessonId").GetInt32());

            // completed means no mistakes left
            Assert.True(doc.RootElement.GetProperty("isCompleted").GetBoolean());

            var mistakeIds = doc.RootElement.GetProperty("mistakeExerciseIds");
            Assert.True(mistakeIds.ValueKind == JsonValueKind.Array);
            Assert.Equal(0, mistakeIds.GetArrayLength());

            Assert.Equal(2, doc.RootElement.GetProperty("totalExercises").GetInt32());
            Assert.Equal(2, doc.RootElement.GetProperty("correctAnswers").GetInt32());
        }

        // next -> Lesson 2 (unlocked by dorobka #1/#2 logic)
        var next2 = await client.GetAsync("/api/next/me");

        Assert.Equal(HttpStatusCode.OK, next2.StatusCode);

        var next2Json = await next2.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(next2Json))
        {
            Assert.Equal("Lesson", doc.RootElement.GetProperty("type").GetString());
            Assert.Equal(2, doc.RootElement.GetProperty("lessonId").GetInt32());
        }
    }
}
