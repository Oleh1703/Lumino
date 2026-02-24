using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class HeartsAndCrystalsHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public HeartsAndCrystalsHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitLesson_WithMistakes_ShouldConsumeHearts()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "test@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow,
                Hearts = 5,
                Crystals = 0,
                Theme = "light"
            });

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", IsPublished = true });
            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "Topic", Order = 1 });
            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "Lesson", Theory = "", Order = 1 });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 1,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q1",
                CorrectAnswer = "a",
                Data = string.Empty,
                Order = 1
            });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 2,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q2",
                CorrectAnswer = "b",
                Data = string.Empty,
                Order = 2
            });

            dbContext.UserLessonProgresses.Add(new UserLessonProgress
            {
                UserId = 10,
                LessonId = 1,
                IsUnlocked = true,
                IsCompleted = false,
                BestScore = 0
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var submit = new
        {
            lessonId = 1,
            answers = new[]
            {
                new { exerciseId = 1, answer = "wrong" },
                new { exerciseId = 2, answer = "b" }
            }
        };

        var response = await client.PostAsJsonAsync("/api/lesson-submit", submit);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();
            var user = dbContext.Users.First(x => x.Id == 10);
            Assert.Equal(4, user.Hearts);
        }
    }

    [Fact]
    public async Task RestoreHearts_ShouldSpendCrystals_AndIncreaseHearts()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Users.Add(new User
            {
                Id = 10,
                Email = "test@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow,
                Hearts = 0,
                Crystals = 20,
                Theme = "light"
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var body = new { heartsToRestore = 2 };

        var response = await client.PostAsJsonAsync("/api/user/restore-hearts", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var root = doc.RootElement;
        Assert.Equal(2, root.GetProperty("hearts").GetInt32());
        Assert.Equal(0, root.GetProperty("crystals").GetInt32());
        Assert.Equal(20, root.GetProperty("spentCrystals").GetInt32());
        Assert.Equal(2, root.GetProperty("restoredHearts").GetInt32());
    }
}
