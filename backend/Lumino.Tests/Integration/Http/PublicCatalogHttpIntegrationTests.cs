using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class PublicCatalogHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public PublicCatalogHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCourses_ShouldReturnOnlyPublishedCourses()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Published", Description = "Desc", IsPublished = true });
            dbContext.Courses.Add(new Course { Id = 2, Title = "Draft", Description = "Desc", IsPublished = false });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        Assert.Single(doc.RootElement.EnumerateArray());

        var course = doc.RootElement[0];
        Assert.Equal(1, course.GetProperty("id").GetInt32());
        Assert.Equal("Published", course.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetTopics_ShouldReturnStableOrder_WhenOrderIsZero_GoesToEnd_ThenById()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 10, CourseId = 1, Title = "T1", Order = 2 });
            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "T2", Order = 0 });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/courses/1/topics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.GetArrayLength());

        Assert.Equal(10, doc.RootElement[0].GetProperty("id").GetInt32());
        Assert.Equal(1, doc.RootElement[1].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task GetLessons_ShouldReturnStableOrder_WhenOrderIsZero_GoesToEnd_ThenById()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 10, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 100, TopicId = 10, Title = "L1", Theory = "", Order = 2 });
            dbContext.Lessons.Add(new Lesson { Id = 90, TopicId = 10, Title = "L2", Theory = "", Order = 0 });
            dbContext.Lessons.Add(new Lesson { Id = 91, TopicId = 10, Title = "L3", Theory = "", Order = 0 });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/topics/10/lessons");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        Assert.Equal(3, doc.RootElement.GetArrayLength());

        // Order=2 first
        Assert.Equal(100, doc.RootElement[0].GetProperty("id").GetInt32());

        // Order=0 goes after, then by Id (90 then 91)
        Assert.Equal(90, doc.RootElement[1].GetProperty("id").GetInt32());
        Assert.Equal(91, doc.RootElement[2].GetProperty("id").GetInt32());
    }
}
