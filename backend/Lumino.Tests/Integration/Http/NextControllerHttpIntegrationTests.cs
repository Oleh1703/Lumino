using System.Net;
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
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/next/me");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
