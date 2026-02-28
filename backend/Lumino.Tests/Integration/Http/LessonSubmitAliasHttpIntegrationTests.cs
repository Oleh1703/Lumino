using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class LessonSubmitAliasHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public LessonSubmitAliasHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitLessonAlias_ShouldWorkLikeLessonSubmit()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });
            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "Lesson", Theory = "", Order = 1 });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 1,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Type",
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
            lessonId = 999, // must be ignored because route has priority
            answers = new[]
            {
                new { exerciseId = 1, answer = "a" }
            }
        };

        var submitJson = JsonSerializer.Serialize(submitPayload);
        var submitContent = new StringContent(submitJson, Encoding.UTF8, "application/json");

        var submitResponse = await client.PostAsync("/api/lessons/1/submit", submitContent);

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
    }
}
