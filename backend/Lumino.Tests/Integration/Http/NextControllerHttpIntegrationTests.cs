using Lumino.Api.Application.DTOs;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class NextControllerHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public NextControllerHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyNext_WhenNoNext_ShouldReturnNoContent()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/next/me");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }



    [Fact]
    public async Task GetMyNext_WhenCourseCompleted_ShouldReturnOk_WithCourseComplete()
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
                Title = "Lesson 1",
                Theory = "",
                Order = 1
            });

            // userId = 10 (TestAuthHandler)
            dbContext.LessonResults.Add(new LessonResult
            {
                Id = 1,
                UserId = 10,
                LessonId = 1,
                Score = 10,
                TotalQuestions = 10,
                MistakesJson = "[]",
                CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/next/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        var next = JsonSerializer.Deserialize<NextActivityResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(next);
        Assert.Equal("CourseComplete", next!.Type);
        Assert.Equal(1, next.CourseId);
    }

    [Fact]
    public async Task GetMyNextPreview_WhenNoNext_ShouldReturnOk_WithProgress()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/next/me/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        var preview = JsonSerializer.Deserialize<NextPreviewResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(preview);
        Assert.NotNull(preview!.Progress);
        Assert.Null(preview.Next);

        // progress must be present and consistent
        Assert.True(preview.Progress.TotalLessons >= 0);
        Assert.True(preview.Progress.TotalScenes >= 0);
    }
}
