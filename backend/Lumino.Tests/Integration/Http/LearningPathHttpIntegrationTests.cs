using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class LearningPathHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public LearningPathHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLearningPath_WhenNoProgress_ShouldCreateProgress_AndReturnFirstLessonUnlocked_SecondLocked()
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

            // Topic 1 (Order=1) + Lesson 1 (Order=1)
            dbContext.Topics.Add(new Topic
            {
                Id = 1,
                CourseId = 1,
                Title = "Topic 1",
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

            // Topic 2 (Order=2) + Lesson 2 (Order=1)
            dbContext.Topics.Add(new Topic
            {
                Id = 2,
                CourseId = 1,
                Title = "Topic 2",
                Order = 2
            });

            dbContext.Lessons.Add(new Lesson
            {
                Id = 2,
                TopicId = 2,
                Title = "Lesson 2",
                Theory = "",
                Order = 1
            });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 2,
                LessonId = 2,
                Type = ExerciseType.Input,
                Question = "Q2",
                Data = "",
                CorrectAnswer = "b",
                Order = 1
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/learning/courses/1/path/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        Assert.Equal(1, doc.RootElement.GetProperty("courseId").GetInt32());

        var topics = doc.RootElement.GetProperty("topics");

        // 2 topics, ordered by Topic.Order
        Assert.Equal(2, topics.GetArrayLength());
        Assert.Equal(1, topics[0].GetProperty("id").GetInt32());
        Assert.Equal(2, topics[1].GetProperty("id").GetInt32());

        var topic1Lessons = topics[0].GetProperty("lessons");
        var topic2Lessons = topics[1].GetProperty("lessons");

        Assert.Equal(1, topic1Lessons.GetArrayLength());
        Assert.Equal(1, topic2Lessons.GetArrayLength());

        var lesson1 = topic1Lessons[0];
        var lesson2 = topic2Lessons[0];

        Assert.Equal(1, lesson1.GetProperty("id").GetInt32());
        Assert.True(lesson1.GetProperty("isUnlocked").GetBoolean());
        Assert.False(lesson1.GetProperty("isPassed").GetBoolean());

        Assert.Equal(2, lesson2.GetProperty("id").GetInt32());
        Assert.False(lesson2.GetProperty("isUnlocked").GetBoolean());
        Assert.False(lesson2.GetProperty("isPassed").GetBoolean());
    }

    [Fact]
    public async Task GetLearningPath_AfterPassingFirstLesson_ShouldUnlockSecondLesson()
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
                Title = "Topic 1",
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

            dbContext.Lessons.Add(new Lesson
            {
                Id = 2,
                TopicId = 1,
                Title = "Lesson 2",
                Theory = "",
                Order = 2
            });

            dbContext.Exercises.Add(new Exercise
            {
                Id = 2,
                LessonId = 2,
                Type = ExerciseType.Input,
                Question = "Q2",
                Data = "",
                CorrectAnswer = "b",
                Order = 1
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        // 1) First call creates progress and unlocks first lesson
        var initialPath = await client.GetAsync("/api/learning/courses/1/path/me");
        Assert.Equal(HttpStatusCode.OK, initialPath.StatusCode);

        // 2) Pass lesson 1
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

        // 3) Path should show lesson 1 passed and lesson 2 unlocked
        var pathResponse = await client.GetAsync("/api/learning/courses/1/path/me");

        Assert.Equal(HttpStatusCode.OK, pathResponse.StatusCode);

        var json = await pathResponse.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        var topics = doc.RootElement.GetProperty("topics");
        var lessons = topics[0].GetProperty("lessons");

        Assert.Equal(2, lessons.GetArrayLength());

        var lesson1 = lessons[0];
        var lesson2 = lessons[1];

        Assert.Equal(1, lesson1.GetProperty("id").GetInt32());
        Assert.True(lesson1.GetProperty("isUnlocked").GetBoolean());
        Assert.True(lesson1.GetProperty("isPassed").GetBoolean());

        Assert.Equal(2, lesson2.GetProperty("id").GetInt32());
        Assert.True(lesson2.GetProperty("isUnlocked").GetBoolean());
        Assert.False(lesson2.GetProperty("isPassed").GetBoolean());
    }

    [Fact]
    public async Task GetLearningPath_ShouldReturnStableOrder_WhenOrderIsZero_ItemsGoToEnd_AndThenById()
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

            // Topic A order=2
            dbContext.Topics.Add(new Topic
            {
                Id = 10,
                CourseId = 1,
                Title = "Topic A",
                Order = 2
            });

            // Topic B order=0 -> should go to end
            dbContext.Topics.Add(new Topic
            {
                Id = 1,
                CourseId = 1,
                Title = "Topic B",
                Order = 0
            });

            

            // Topic B must have at least one lesson to appear in path (Topic join Lesson)
            dbContext.Lessons.Add(new Lesson
            {
                Id = 200,
                TopicId = 1,
                Title = "LB1",
                Theory = "",
                Order = 0
            });

            // Lessons inside Topic A: Lesson with Order=0 should go to end, then by Id
            dbContext.Lessons.Add(new Lesson
            {
                Id = 100,
                TopicId = 10,
                Title = "L1",
                Theory = "",
                Order = 1
            });

            dbContext.Lessons.Add(new Lesson
            {
                Id = 90,
                TopicId = 10,
                Title = "L2",
                Theory = "",
                Order = 0
            });

            dbContext.Lessons.Add(new Lesson
            {
                Id = 91,
                TopicId = 10,
                Title = "L3",
                Theory = "",
                Order = 0
            });

            // Ensure totalQuestions not null for percent calc
            dbContext.Exercises.Add(new Exercise { Id = 1, LessonId = 100, Type = ExerciseType.Input, Question = "Q", Data = "", CorrectAnswer = "a", Order = 1 });
            dbContext.Exercises.Add(new Exercise { Id = 2, LessonId = 90, Type = ExerciseType.Input, Question = "Q", Data = "", CorrectAnswer = "a", Order = 1 });
            dbContext.Exercises.Add(new Exercise { Id = 3, LessonId = 91, Type = ExerciseType.Input, Question = "Q", Data = "", CorrectAnswer = "a", Order = 1 });
            dbContext.Exercises.Add(new Exercise { Id = 4, LessonId = 200, Type = ExerciseType.Input, Question = "Q", Data = "", CorrectAnswer = "a", Order = 1 });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/learning/courses/1/path/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        var topics = doc.RootElement.GetProperty("topics");

        // Topic A (Order=2) must be before Topic B (Order=0)
        Assert.Equal(2, topics.GetArrayLength());
        Assert.Equal(10, topics[0].GetProperty("id").GetInt32());
        Assert.Equal(1, topics[1].GetProperty("id").GetInt32());

        var lessons = topics[0].GetProperty("lessons");

        Assert.Equal(3, lessons.GetArrayLength());

        // Order=1 first
        Assert.Equal(100, lessons[0].GetProperty("id").GetInt32());

        // Order=0 goes after, then by Id (90 then 91)
        Assert.Equal(90, lessons[1].GetProperty("id").GetInt32());
        Assert.Equal(91, lessons[2].GetProperty("id").GetInt32());
    }
}
