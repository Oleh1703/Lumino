using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class LessonAndExercisesLockHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public LessonAndExercisesLockHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLessonAndExercises_WhenLessonLocked_ShouldReturnForbidden()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "", Order = 1 });
            dbContext.Lessons.Add(new Lesson { Id = 2, TopicId = 1, Title = "L2", Theory = "", Order = 2 });

            dbContext.Exercises.Add(new Exercise { Id = 1, LessonId = 1, Type = ExerciseType.Input, Question = "Q1", Data = "", CorrectAnswer = "a", Order = 1 });
            dbContext.Exercises.Add(new Exercise { Id = 2, LessonId = 2, Type = ExerciseType.Input, Question = "Q2", Data = "", CorrectAnswer = "b", Order = 1 });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        // Lesson 2 should still be locked, so LessonById and Exercises endpoints must return Forbidden
        var lessonResponse = await client.GetAsync("/api/lessons/2");
        Assert.Equal(HttpStatusCode.Forbidden, lessonResponse.StatusCode);

        var exercisesResponse = await client.GetAsync("/api/lessons/2/exercises");
        Assert.Equal(HttpStatusCode.Forbidden, exercisesResponse.StatusCode);
    }

    [Fact]
    public async Task GetExercises_WhenUnlocked_ShouldReturnExercisesInStableOrder()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            dbContext.Courses.Add(new Course { Id = 1, Title = "Course", Description = "Desc", IsPublished = true });

            dbContext.Topics.Add(new Topic { Id = 1, CourseId = 1, Title = "Topic", Order = 1 });

            dbContext.Lessons.Add(new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "", Order = 1 });

            // Order=2 first, then Order=0 to end (by Id)
            dbContext.Exercises.Add(new Exercise { Id = 100, LessonId = 1, Type = ExerciseType.Input, Question = "Q1", Data = "", CorrectAnswer = "a", Order = 2 });
            dbContext.Exercises.Add(new Exercise { Id = 90, LessonId = 1, Type = ExerciseType.Input, Question = "Q2", Data = "", CorrectAnswer = "a", Order = 0 });
            dbContext.Exercises.Add(new Exercise { Id = 91, LessonId = 1, Type = ExerciseType.Input, Question = "Q3", Data = "", CorrectAnswer = "a", Order = 0 });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var response = await client.GetAsync("/api/lessons/1/exercises");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        // Expect exercise 100 first, then 90, then 91
        var idx100 = json.IndexOf("\"id\":100");
        var idx90 = json.IndexOf("\"id\":90");
        var idx91 = json.IndexOf("\"id\":91");

        Assert.True(idx100 >= 0);
        Assert.True(idx90 >= 0);
        Assert.True(idx91 >= 0);

        Assert.True(idx100 < idx90);
        Assert.True(idx90 < idx91);
    }
}
