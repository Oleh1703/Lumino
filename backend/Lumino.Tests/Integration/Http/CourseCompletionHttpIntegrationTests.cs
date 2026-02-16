using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class CourseCompletionHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public CourseCompletionHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartCourse_ThenCompletion_ShouldBeInProgress()
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

            dbContext.Exercises.Add(new Exercise
            {
                Id = 1,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q1",
                Data = "",
                CorrectAnswer = "a",
                Order = 1
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var completionResponse = await client.GetAsync("/api/learning/courses/1/completion/me");

        Assert.Equal(HttpStatusCode.OK, completionResponse.StatusCode);

        var json = await completionResponse.Content.ReadAsStringAsync();

        Assert.Contains("\"courseId\":1", json);
        Assert.Contains("\"status\":\"InProgress\"", json);
        Assert.Contains("\"isCompleted\":false", json);
        Assert.Contains("\"totalLessons\":1", json);
        Assert.Contains("\"completedLessons\":0", json);
        Assert.Contains("\"completionPercent\":0", json);
        Assert.Contains("\"nextLessonId\":1", json);
    }

    [Fact]
    public async Task StartCourse_PassLastLesson_ThenCompletion_ShouldBeCompleted()
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

            dbContext.Exercises.Add(new Exercise
            {
                Id = 1,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q1",
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
            lessonId = 1,
            answers = new[]
            {
                new { exerciseId = 1, answer = "a" }
            }
        };

        var submitJson = JsonSerializer.Serialize(submitPayload);
        var submitContent = new StringContent(submitJson, Encoding.UTF8, "application/json");

        var submitResponse = await client.PostAsync("/api/lesson-submit", submitContent);

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

        var completionResponse = await client.GetAsync("/api/learning/courses/1/completion/me");

        Assert.Equal(HttpStatusCode.OK, completionResponse.StatusCode);

        var json = await completionResponse.Content.ReadAsStringAsync();

        Assert.Contains("\"courseId\":1", json);
        Assert.Contains("\"status\":\"Completed\"", json);
        Assert.Contains("\"isCompleted\":true", json);
        Assert.Contains("\"totalLessons\":1", json);
        Assert.Contains("\"completedLessons\":1", json);
        Assert.Contains("\"completionPercent\":100", json);
        Assert.Contains("\"nextLessonId\":null", json);
        Assert.Contains("\"remainingLessonIds\":[]", json);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            var userCourse = dbContext.UserCourses.FirstOrDefault(x => x.UserId == 10 && x.CourseId == 1);

            Assert.NotNull(userCourse);
            Assert.True(userCourse!.IsCompleted);
            Assert.False(userCourse.IsActive);
            Assert.Null(userCourse.LastLessonId);
        }
    }
}
