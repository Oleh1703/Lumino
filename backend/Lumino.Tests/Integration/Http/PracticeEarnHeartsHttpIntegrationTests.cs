using Lumino.Api.Application.DTOs;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class PracticeEarnHeartsHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public PracticeEarnHeartsHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitLessonMistakes_WhenPracticeCompleted_ShouldAwardOneHeart_AndNotAwardTwice()
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
                Role = Role.User,
                CreatedAt = DateTime.UtcNow,
                Hearts = 0,
                Crystals = 0,
                Theme = "light",
                HeartsUpdatedAtUtc = DateTime.UtcNow
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

            var details = new LessonResultDetailsJson
            {
                PracticeHeartGranted = false,
                MistakeExerciseIds = new List<int> { 2 },
                Answers = new List<LessonAnswerResultDto>
                {
                    new LessonAnswerResultDto
                    {
                        ExerciseId = 1,
                        UserAnswer = "a",
                        CorrectAnswer = "a",
                        IsCorrect = true
                    },
                    new LessonAnswerResultDto
                    {
                        ExerciseId = 2,
                        UserAnswer = "WRONG",
                        CorrectAnswer = "b",
                        IsCorrect = false
                    }
                }
            };

            dbContext.LessonResults.Add(new LessonResult
            {
                Id = 1,
                UserId = 10,
                LessonId = 1,
                Score = 1,
                TotalQuestions = 2,
                MistakesJson = JsonSerializer.Serialize(details),
                CompletedAt = DateTime.UtcNow
            });

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        var submit = new
        {
            idempotencyKey = "practice-1",
            answers = new[]
            {
                new { exerciseId = 2, answer = "b" }
            }
        };

        var response1 = await client.PostAsJsonAsync("/api/lessons/1/mistakes/submit", submit);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var body1 = await response1.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(body1))
        {
            Assert.True(doc.RootElement.GetProperty("isCompleted").GetBoolean());
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();
            var user = dbContext.Users.First(x => x.Id == 10);
            Assert.Equal(1, user.Hearts);
        }

        // second submit (should not award extra heart)
        var response2 = await client.PostAsJsonAsync("/api/lessons/1/mistakes/submit", submit);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();
            var user = dbContext.Users.First(x => x.Id == 10);
            Assert.Equal(1, user.Hearts);
        }
    }
}
