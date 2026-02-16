using Lumino.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class LessonSubmitValidationHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public LessonSubmitValidationHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitLesson_WhenLessonIdInvalid_ShouldReturnBadRequest()
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

        Assert.Contains("\"type\":\"bad_request\"", body);
        Assert.Contains("LessonId is invalid", body);
    }

    [Fact]
    public async Task SubmitLesson_WhenAnswersEmpty_ShouldReturnBadRequest()
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
            lessonId = 1,
            answers = Array.Empty<object>()
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/lesson-submit", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"type\":\"bad_request\"", body);
        Assert.Contains("Answers are required", body);
    }
}
