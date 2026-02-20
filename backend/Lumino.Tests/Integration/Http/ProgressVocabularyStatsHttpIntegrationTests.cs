using Lumino.Api.Application.DTOs;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class ProgressVocabularyStatsHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ProgressVocabularyStatsHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyProgress_ShouldIncludeVocabularyStats()
    {
        var nowUtc = DateTime.UtcNow;
        var userId = 10; // must match TestAuthHandler (ClaimTypes.NameIdentifier)

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = userId,
                Email = "user@test.com",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = nowUtc
            });

            dbContext.VocabularyItems.AddRange(
                new VocabularyItem { Id = 1, Word = "one", Translation = "один", Example = null },
                new VocabularyItem { Id = 2, Word = "two", Translation = "два", Example = null },
                new VocabularyItem { Id = 3, Word = "three", Translation = "три", Example = null }
            );

            dbContext.UserVocabularies.AddRange(
                new UserVocabulary
                {
                    Id = 1,
                    UserId = userId,
                    VocabularyItemId = 1,
                    AddedAt = nowUtc.AddDays(-5),
                    LastReviewedAt = null,
                    NextReviewAt = nowUtc.AddDays(-1),
                    ReviewCount = 0
                },
                new UserVocabulary
                {
                    Id = 2,
                    UserId = userId,
                    VocabularyItemId = 2,
                    AddedAt = nowUtc.AddDays(-4),
                    LastReviewedAt = nowUtc.AddDays(-2),
                    NextReviewAt = nowUtc.AddHours(-2),
                    ReviewCount = 2
                },
                new UserVocabulary
                {
                    Id = 3,
                    UserId = userId,
                    VocabularyItemId = 3,
                    AddedAt = nowUtc.AddDays(-3),
                    LastReviewedAt = nowUtc.AddDays(-1),
                    NextReviewAt = nowUtc.AddDays(2),
                    ReviewCount = 3
                }
            );

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/progress/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        var progress = JsonSerializer.Deserialize<UserProgressResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(progress);

        Assert.Equal(3, progress!.TotalVocabulary);
        Assert.Equal(2, progress.DueVocabulary);

        Assert.NotNull(progress.NextVocabularyReviewAt);

        // earliest NextReviewAt should be returned
        Assert.Equal(nowUtc.AddDays(-1).Date, progress.NextVocabularyReviewAt!.Value.Date);
    }
}
