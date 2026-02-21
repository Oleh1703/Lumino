using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class DemoApiHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public DemoApiHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DemoNext_ShouldReturnLessonByStep_AndReturn404WhenOutOfRange()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 10, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 10, Title = "Demo 1", Theory = "", Order = 1 });
            dbContext.Lessons.Add(new Lesson { Id = 2, TopicId = 10, Title = "Demo 2", Theory = "", Order = 2 });
            dbContext.Lessons.Add(new Lesson { Id = 3, TopicId = 10, Title = "Demo 3", Theory = "", Order = 3 });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var okResponse = await client.GetAsync("/api/demo/next?step=1");

        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);

        var okJson = await okResponse.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(okJson))
        {
            Assert.Equal(1, doc.RootElement.GetProperty("step").GetInt32());
            Assert.Equal(2, doc.RootElement.GetProperty("stepNumber").GetInt32());
            Assert.Equal(3, doc.RootElement.GetProperty("total").GetInt32());
            Assert.False(doc.RootElement.GetProperty("isLast").GetBoolean());
            Assert.Equal(string.Empty, doc.RootElement.GetProperty("ctaText").GetString());
            Assert.False(doc.RootElement.GetProperty("showRegisterCta").GetBoolean());
            Assert.Equal("Урок 2 з 3", doc.RootElement.GetProperty("lessonNumberText").GetString());

            var lesson = doc.RootElement.GetProperty("lesson");
            Assert.Equal(2, lesson.GetProperty("id").GetInt32());
        }

        var lastResponse = await client.GetAsync("/api/demo/next?step=2");

        Assert.Equal(HttpStatusCode.OK, lastResponse.StatusCode);

        var lastJson = await lastResponse.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(lastJson))
        {
            Assert.Equal(2, doc.RootElement.GetProperty("step").GetInt32());
            Assert.Equal(3, doc.RootElement.GetProperty("stepNumber").GetInt32());
            Assert.Equal(3, doc.RootElement.GetProperty("total").GetInt32());
            Assert.True(doc.RootElement.GetProperty("isLast").GetBoolean());
            Assert.Equal("Щоб зберегти прогрес — зареєструйся", doc.RootElement.GetProperty("ctaText").GetString());
            Assert.True(doc.RootElement.GetProperty("showRegisterCta").GetBoolean());
            Assert.Equal("Урок 3 з 3", doc.RootElement.GetProperty("lessonNumberText").GetString());

            var lesson = doc.RootElement.GetProperty("lesson");
            Assert.Equal(3, lesson.GetProperty("id").GetInt32());
        }

        var notFoundResponse = await client.GetAsync("/api/demo/next?step=999");

        Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);

        var nfJson = await notFoundResponse.Content.ReadAsStringAsync();

        using var nfDoc = JsonDocument.Parse(nfJson);
        Assert.Equal("not_found", nfDoc.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task DemoNextPack_ShouldReturnLessonAndExercisesWithMeta()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 10, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 10, Title = "Demo 1", Theory = "", Order = 1 });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 101,
                LessonId = 1,
                Type = ExerciseType.MultipleChoice,
                Question = "Choose",
                Data = "[]",
                CorrectAnswer = "cat",
                Order = 1
            });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 102,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Type",
                Data = "",
                CorrectAnswer = "dog",
                Order = 2
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/demo/next-pack?step=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        Assert.Equal(0, doc.RootElement.GetProperty("step").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("stepNumber").GetInt32());
        Assert.Equal(3, doc.RootElement.GetProperty("total").GetInt32());

        var lesson = doc.RootElement.GetProperty("lesson");
        Assert.Equal(1, lesson.GetProperty("id").GetInt32());

        var exercises = doc.RootElement.GetProperty("exercises");
        Assert.Equal(2, exercises.GetArrayLength());
    }

    [Fact]
    public async Task DemoLessons_ShouldReturnConfiguredLessons_AndNotRequireProgressTables()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 10, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 10, Title = "Demo 1", Theory = "", Order = 1 });
            dbContext.Lessons.Add(new Lesson { Id = 2, TopicId = 10, Title = "Demo 2", Theory = "", Order = 2 });
            dbContext.Lessons.Add(new Lesson { Id = 3, TopicId = 10, Title = "Demo 3", Theory = "", Order = 3 });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/demo/lessons");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        Assert.Equal(3, doc.RootElement.GetArrayLength());
        Assert.Equal(1, doc.RootElement[0].GetProperty("id").GetInt32());
        Assert.Equal(2, doc.RootElement[1].GetProperty("id").GetInt32());
        Assert.Equal(3, doc.RootElement[2].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task DemoExercises_AndSubmit_ShouldNotWriteLessonResultsOrProgress()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 10, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 10, Title = "Demo 1", Theory = "", Order = 1 });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 101,
                LessonId = 1,
                Type = ExerciseType.MultipleChoice,
                Question = "Choose",
                Data = "[]",
                CorrectAnswer = "cat",
                Order = 1
            });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 102,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Type",
                Data = "",
                CorrectAnswer = "dog",
                Order = 2
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var exercisesResponse = await client.GetAsync("/api/demo/lessons/1/exercises");

        Assert.Equal(HttpStatusCode.OK, exercisesResponse.StatusCode);

        var submitPayload = new
        {
            lessonId = 1,
            idempotencyKey = "demo-1",
            answers = new[]
            {
                new { exerciseId = 101, answer = "cat" },
                new { exerciseId = 102, answer = "dog" }
            }
        };

        var submitJson = JsonSerializer.Serialize(submitPayload);

        var submitResponse = await client.PostAsync(
            "/api/demo/lesson-submit",
            new StringContent(submitJson, Encoding.UTF8, "application/json")
        );

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var responseJson = await submitResponse.Content.ReadAsStringAsync();

        using (var doc = JsonDocument.Parse(responseJson))
        {
            Assert.Equal(2, doc.RootElement.GetProperty("totalExercises").GetInt32());
            Assert.Equal(2, doc.RootElement.GetProperty("correctAnswers").GetInt32());
            Assert.True(doc.RootElement.GetProperty("isPassed").GetBoolean());

            var answers = doc.RootElement.GetProperty("answers");
            Assert.Equal(2, answers.GetArrayLength());
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            Assert.Equal(0, dbContext.LessonResults.Count());
            Assert.Equal(0, dbContext.UserProgresses.Count());
            Assert.Equal(0, dbContext.UserLessonProgresses.Count());
        }
    }

    [Fact]
    public async Task DemoEndpoints_ShouldForbidLessonOutsideAllowList()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 10, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 99, TopicId = 10, Title = "Not demo", Theory = "", Order = 99 });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 9901,
                LessonId = 99,
                Type = ExerciseType.Input,
                Question = "Type",
                Data = "",
                CorrectAnswer = "ok",
                Order = 1
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/demo/lessons/99/exercises");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        Assert.Equal("forbidden", doc.RootElement.GetProperty("type").GetString());
    }
}
