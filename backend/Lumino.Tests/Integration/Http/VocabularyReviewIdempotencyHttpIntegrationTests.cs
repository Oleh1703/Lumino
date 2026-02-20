using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class VocabularyReviewIdempotencyHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public VocabularyReviewIdempotencyHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Review_TwiceWithSameIdempotencyKey_ShouldApplyOnlyOnce()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.VocabularyItems.Add(new VocabularyItem
            {
                Id = 1,
                Word = "apple",
                Translation = "яблуко",
                Example = "I eat an apple"
            });

            dbContext.UserVocabularies.Add(new UserVocabulary
            {
                Id = 1,
                UserId = 10,
                VocabularyItemId = 1,
                AddedAt = DateTime.UtcNow.AddDays(-1),
                LastReviewedAt = null,
                NextReviewAt = DateTime.UtcNow.AddMinutes(-5),
                ReviewCount = 0,
                ReviewIdempotencyKey = null
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var payload = new
        {
            isCorrect = true,
            idempotencyKey = "k-vocab-1"
        };

        var json = JsonSerializer.Serialize(payload);

        var response1 = await client.PostAsync(
            "/api/vocabulary/1/review",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var response2 = await client.PostAsync(
            "/api/vocabulary/1/review",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var body1 = await response1.Content.ReadAsStringAsync();
        var body2 = await response2.Content.ReadAsStringAsync();

        using var doc1 = JsonDocument.Parse(body1);
        using var doc2 = JsonDocument.Parse(body2);

        Assert.Equal(
            doc1.RootElement.GetProperty("reviewCount").GetInt32(),
            doc2.RootElement.GetProperty("reviewCount").GetInt32());

        Assert.Equal(
            doc1.RootElement.GetProperty("nextReviewAt").GetDateTime(),
            doc2.RootElement.GetProperty("nextReviewAt").GetDateTime());

        Assert.Equal(
            doc1.RootElement.GetProperty("lastReviewedAt").GetDateTime(),
            doc2.RootElement.GetProperty("lastReviewedAt").GetDateTime());

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            var entity = dbContext.UserVocabularies.Single(x => x.Id == 1 && x.UserId == 10);

            Assert.Equal(1, entity.ReviewCount);
            Assert.Equal("k-vocab-1", entity.ReviewIdempotencyKey);
        }
    }
}
