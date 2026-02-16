using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class ActiveCourseHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ActiveCourseHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetActiveCourse_WhenNoActiveCourse_ShouldReturnNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/learning/courses/active");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StartCourse_ThenGetActiveCourse_ShouldReturnOkAndActiveCourse()
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

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var activeResponse = await client.GetAsync("/api/learning/courses/active");

        Assert.Equal(HttpStatusCode.OK, activeResponse.StatusCode);

        var json = await activeResponse.Content.ReadAsStringAsync();

        Assert.Contains("\"courseId\":1", json);
        Assert.Contains("\"lastLessonId\":1", json);
    }
}
