using Lumino.Api.Application.DTOs;
using Lumino.Api.Data;
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
