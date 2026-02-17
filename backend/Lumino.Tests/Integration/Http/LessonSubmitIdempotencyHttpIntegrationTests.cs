using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class LessonSubmitIdempotencyHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public LessonSubmitIdempotencyHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitLesson_TwiceWithSameIdempotencyKey_ShouldCreateOnlyOneLessonResult()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "", Order = 1 });

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

        var submitPayload = new
        {
            lessonId = 1,
            idempotencyKey = "k-lesson-1",
            answers = new[]
            {
                new { exerciseId = 1, answer = "a" }
            }
        };

        var submitJson = JsonSerializer.Serialize(submitPayload);

        var submitResponse1 = await client.PostAsync(
            "/api/lesson-submit",
            new StringContent(submitJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, submitResponse1.StatusCode);

        var submitResponse2 = await client.PostAsync(
            "/api/lesson-submit",
            new StringContent(submitJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, submitResponse2.StatusCode);

        var json1 = await submitResponse1.Content.ReadAsStringAsync();
        var json2 = await submitResponse2.Content.ReadAsStringAsync();

        using var doc1 = JsonDocument.Parse(json1);
        using var doc2 = JsonDocument.Parse(json2);

        Assert.Equal(
            doc1.RootElement.GetProperty("totalExercises").GetInt32(),
            doc2.RootElement.GetProperty("totalExercises").GetInt32());

        Assert.Equal(
            doc1.RootElement.GetProperty("correctAnswers").GetInt32(),
            doc2.RootElement.GetProperty("correctAnswers").GetInt32());

        Assert.Equal(
            doc1.RootElement.GetProperty("isPassed").GetBoolean(),
            doc2.RootElement.GetProperty("isPassed").GetBoolean());

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            var resultsWithKey = dbContext.LessonResults
                .Where(x => x.UserId == 10 && x.LessonId == 1 && x.IdempotencyKey == "k-lesson-1")
                .ToList();

            Assert.Single(resultsWithKey);

            var allResultsForLesson = dbContext.LessonResults
                .Where(x => x.UserId == 10 && x.LessonId == 1)
                .ToList();

            Assert.Single(allResultsForLesson);
        }
    }
}
