using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class LessonProgressHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public LessonProgressHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartCourse_ThenGetLessonProgress_ShouldReturnAllLessons_WithFirstUnlocked_OthersLocked()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "", Order = 1 });
            dbContext.Lessons.Add(new Lesson { Id = 2, TopicId = 1, Title = "L2", Theory = "", Order = 2 });
            dbContext.Lessons.Add(new Lesson { Id = 3, TopicId = 1, Title = "L3", Theory = "", Order = 3 });

            dbContext.Exercises.Add(new Exercise { Id = 1, LessonId = 1, Type = ExerciseType.Input, Question = "Q1", Data = "", CorrectAnswer = "a", Order = 1 });
            dbContext.Exercises.Add(new Exercise { Id = 2, LessonId = 2, Type = ExerciseType.Input, Question = "Q2", Data = "", CorrectAnswer = "a", Order = 1 });
            dbContext.Exercises.Add(new Exercise { Id = 3, LessonId = 3, Type = ExerciseType.Input, Question = "Q3", Data = "", CorrectAnswer = "a", Order = 1 });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var progressResponse = await client.GetAsync("/api/learning/courses/1/lessons/progress");

        Assert.Equal(HttpStatusCode.OK, progressResponse.StatusCode);

        var json = await progressResponse.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        Assert.Equal(3, doc.RootElement.GetArrayLength());

        var p1 = doc.RootElement[0];
        var p2 = doc.RootElement[1];
        var p3 = doc.RootElement[2];

        Assert.Equal(1, p1.GetProperty("lessonId").GetInt32());
        Assert.True(p1.GetProperty("isUnlocked").GetBoolean());
        Assert.False(p1.GetProperty("isCompleted").GetBoolean());

        Assert.Equal(2, p2.GetProperty("lessonId").GetInt32());
        Assert.False(p2.GetProperty("isUnlocked").GetBoolean());
        Assert.False(p2.GetProperty("isCompleted").GetBoolean());

        Assert.Equal(3, p3.GetProperty("lessonId").GetInt32());
        Assert.False(p3.GetProperty("isUnlocked").GetBoolean());
        Assert.False(p3.GetProperty("isCompleted").GetBoolean());
    }

    [Fact]
    public async Task StartCourse_PassFirstLesson_ThenSecondLessonShouldBeUnlocked()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "", Order = 1 });
            dbContext.Lessons.Add(new Lesson { Id = 2, TopicId = 1, Title = "L2", Theory = "", Order = 2 });

            dbContext.Exercises.Add(new Exercise { Id = 1, LessonId = 1, Type = ExerciseType.Input, Question = "Q1", Data = "", CorrectAnswer = "a", Order = 1 });
            dbContext.Exercises.Add(new Exercise { Id = 2, LessonId = 2, Type = ExerciseType.Input, Question = "Q2", Data = "", CorrectAnswer = "b", Order = 1 });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var submitPayload = new
        {
            lessonId = 1,
            answers = new[]
            {
                new { exerciseId = 1, answer = "a" }
            }
        };

        var submitJson = JsonSerializer.Serialize(submitPayload);
        var submitContent = new StringContent(submitJson, Encoding.UTF8, "application/json");

        var submitResponse = await client.PostAsync("/api/lesson-submit", submitContent);

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var progressResponse = await client.GetAsync("/api/learning/courses/1/lessons/progress");

        Assert.Equal(HttpStatusCode.OK, progressResponse.StatusCode);

        var json = await progressResponse.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.GetArrayLength());

        var p1 = doc.RootElement[0];
        var p2 = doc.RootElement[1];

        Assert.Equal(1, p1.GetProperty("lessonId").GetInt32());
        Assert.True(p1.GetProperty("isUnlocked").GetBoolean());
        Assert.True(p1.GetProperty("isCompleted").GetBoolean());

        Assert.Equal(2, p2.GetProperty("lessonId").GetInt32());
        Assert.True(p2.GetProperty("isUnlocked").GetBoolean());
        Assert.False(p2.GetProperty("isCompleted").GetBoolean());
    }
}
