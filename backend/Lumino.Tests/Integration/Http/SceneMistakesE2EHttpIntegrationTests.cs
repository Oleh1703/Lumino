using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class SceneMistakesE2EHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public SceneMistakesE2EHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartCourse_PassLesson_UnlockScene_SubmitWithMistake_RepeatMistakes_CompleteScene_ShouldWorkAndKeepDtoContract()
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
                CreatedAt = DateTime.UtcNow
            });

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

            dbContext.Lessons.AddRange(
                new Lesson { Id = 1, TopicId = 1, Title = "L1", Theory = "", Order = 1 },
                new Lesson { Id = 2, TopicId = 1, Title = "L2", Theory = "", Order = 2 }
            );

            dbContext.Exercises.AddRange(
                new Exercise { Id = 1, LessonId = 1, Type = ExerciseType.Input, Question = "Q1", Data = "", CorrectAnswer = "a", Order = 1 },
                new Exercise { Id = 2, LessonId = 2, Type = ExerciseType.Input, Question = "Q2", Data = "", CorrectAnswer = "a", Order = 1 }
            );

            dbContext.Scenes.AddRange(
                new Scene
                {
                    Id = 1,
                    CourseId = 1,
                    Order = 1,
                    Title = "Scene 1",
                    Description = "Desc",
                    SceneType = "Dialog",
                    BackgroundUrl = null,
                    AudioUrl = null
                },
                new Scene
                {
                    Id = 2,
                    CourseId = 1,
                    Order = 2,
                    Title = "Scene 2",
                    Description = "Desc",
                    SceneType = "Quiz",
                    BackgroundUrl = null,
                    AudioUrl = null
                }
            );

            dbContext.SceneSteps.AddRange(
                new SceneStep
                {
                    Id = 21,
                    SceneId = 2,
                    Order = 1,
                    Speaker = "A",
                    Text = "Pick A",
                    StepType = "Choice",
                    MediaUrl = null,
                    ChoicesJson = "[{\"text\":\"A\",\"isCorrect\":true},{\"text\":\"B\",\"isCorrect\":false}]"
                },
                new SceneStep
                {
                    Id = 22,
                    SceneId = 2,
                    Order = 2,
                    Speaker = "A",
                    Text = "Pick C",
                    StepType = "Choice",
                    MediaUrl = null,
                    ChoicesJson = "[{\"text\":\"C\",\"isCorrect\":true},{\"text\":\"D\",\"isCorrect\":false}]"
                }
            );

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        // 1) Start course
        var startResponse = await client.PostAsync("/api/learning/courses/1/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        // 2) Scene 2 must be locked before any passed lessons
        var beforeUnlockDetails = await client.GetAsync("/api/scenes/2");
        Assert.Equal(HttpStatusCode.OK, beforeUnlockDetails.StatusCode);

        var beforeUnlockJson = await beforeUnlockDetails.Content.ReadAsStringAsync();
        using (var beforeUnlockDoc = JsonDocument.Parse(beforeUnlockJson))
        {
            var root = beforeUnlockDoc.RootElement;

            AssertJsonHasProperties(root, "id", "courseId", "order", "title", "isUnlocked", "isCompleted");

            Assert.Equal(2, GetInt32PropertyIgnoreCase(root, "id"));
            Assert.False(GetBoolPropertyIgnoreCase(root, "isUnlocked"));
        }

        // 3) Pass lesson 1 => unlock scene 2
        var submitLessonRequest = new
        {
            lessonId = 1,
            idempotencyKey = "lesson-key-1",
            answers = new[]
            {
                new { exerciseId = 1, answer = "a" }
            }
        };

        var submitLessonJson = JsonSerializer.Serialize(submitLessonRequest);
        var submitLessonResponse = await client.PostAsync("/api/lesson-submit", new StringContent(submitLessonJson, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, submitLessonResponse.StatusCode);

        // 4) Scene 2 should be unlocked now
        var afterUnlockDetails = await client.GetAsync("/api/scenes/2");
        Assert.Equal(HttpStatusCode.OK, afterUnlockDetails.StatusCode);

        var afterUnlockJson = await afterUnlockDetails.Content.ReadAsStringAsync();
        using (var afterUnlockDoc = JsonDocument.Parse(afterUnlockJson))
        {
            var root = afterUnlockDoc.RootElement;

            AssertJsonHasProperties(root, "id", "courseId", "order", "title", "isUnlocked", "isCompleted");

            Assert.Equal(2, GetInt32PropertyIgnoreCase(root, "id"));
            Assert.True(GetBoolPropertyIgnoreCase(root, "isUnlocked"));
        }

        // 5) Get content => must include steps
        var contentResponse = await client.GetAsync("/api/scenes/2/content");
        Assert.Equal(HttpStatusCode.OK, contentResponse.StatusCode);

        var contentJson = await contentResponse.Content.ReadAsStringAsync();
        using (var contentDoc = JsonDocument.Parse(contentJson))
        {
            var root = contentDoc.RootElement;

            AssertJsonHasProperties(root, "id", "steps");

            Assert.Equal(2, GetInt32PropertyIgnoreCase(root, "id"));

            var steps = GetPropertyIgnoreCase(root, "steps");
            Assert.Equal(JsonValueKind.Array, steps.ValueKind);
            Assert.Equal(2, steps.GetArrayLength());

            AssertJsonHasProperties(steps[0], "id", "order", "stepType");
        }

        // 6) Submit scene with one mistake (21 wrong, 22 correct) => not completed
        var submitSceneRequest1 = new
        {
            idempotencyKey = "scene-key-1",
            answers = new[]
            {
                new { stepId = 21, answer = "B" },
                new { stepId = 22, answer = "C" }
            }
        };

        var submitSceneJson1 = JsonSerializer.Serialize(submitSceneRequest1);
        var submitSceneResponse1 = await client.PostAsync("/api/scenes/2/submit", new StringContent(submitSceneJson1, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, submitSceneResponse1.StatusCode);

        var submitSceneBody1 = await submitSceneResponse1.Content.ReadAsStringAsync();
        using (var submitSceneDoc1 = JsonDocument.Parse(submitSceneBody1))
        {
            var root = submitSceneDoc1.RootElement;

            AssertJsonHasAnyProperty(root, "sceneId", "id");
            AssertJsonHasProperties(root, "totalQuestions", "correctAnswers", "isCompleted", "mistakeStepIds");

            Assert.Equal(2, GetSceneId(root));
            Assert.Equal(2, GetInt32PropertyIgnoreCase(root, "totalQuestions"));
            Assert.Equal(1, GetInt32PropertyIgnoreCase(root, "correctAnswers"));
            Assert.False(GetBoolPropertyIgnoreCase(root, "isCompleted"));

            var mistakes = GetPropertyIgnoreCase(root, "mistakeStepIds");
            Assert.Equal(JsonValueKind.Array, mistakes.ValueKind);
            Assert.Equal(1, mistakes.GetArrayLength());
        }

        // 7) Get mistakes => must contain step 21
        var mistakesResponse = await client.GetAsync("/api/scenes/2/mistakes");
        Assert.Equal(HttpStatusCode.OK, mistakesResponse.StatusCode);

        var mistakesJson = await mistakesResponse.Content.ReadAsStringAsync();
        using (var mistakesDoc = JsonDocument.Parse(mistakesJson))
        {
            var root = mistakesDoc.RootElement;

            AssertJsonHasAnyProperty(root, "sceneId", "id");
            AssertJsonHasProperties(root, "totalMistakes", "mistakeStepIds", "steps");

            Assert.Equal(2, GetSceneId(root));
            Assert.Equal(1, GetInt32PropertyIgnoreCase(root, "totalMistakes"));

            var mistakeIds = GetPropertyIgnoreCase(root, "mistakeStepIds");
            Assert.Equal(JsonValueKind.Array, mistakeIds.ValueKind);
            Assert.Equal(1, mistakeIds.GetArrayLength());

            var steps = GetPropertyIgnoreCase(root, "steps");
            Assert.Equal(JsonValueKind.Array, steps.ValueKind);
            Assert.Equal(1, steps.GetArrayLength());
        }

        // 8) Submit mistakes correct => completed
        var submitMistakesRequest = new
        {
            idempotencyKey = "scene-mistakes-key-1",
            answers = new[]
            {
                new { stepId = 21, answer = "A" }
            }
        };

        var submitMistakesJson = JsonSerializer.Serialize(submitMistakesRequest);
        var submitMistakesResponse = await client.PostAsync("/api/scenes/2/mistakes/submit", new StringContent(submitMistakesJson, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, submitMistakesResponse.StatusCode);

        var submitMistakesBody = await submitMistakesResponse.Content.ReadAsStringAsync();
        using (var submitMistakesDoc = JsonDocument.Parse(submitMistakesBody))
        {
            var root = submitMistakesDoc.RootElement;

            AssertJsonHasAnyProperty(root, "sceneId", "id");
            AssertJsonHasProperties(root, "totalQuestions", "correctAnswers", "isCompleted", "mistakeStepIds");

            Assert.Equal(2, GetSceneId(root));
            Assert.True(GetBoolPropertyIgnoreCase(root, "isCompleted"));

            var mistakeIds = GetPropertyIgnoreCase(root, "mistakeStepIds");
            Assert.Equal(JsonValueKind.Array, mistakeIds.ValueKind);
            Assert.Equal(0, mistakeIds.GetArrayLength());
        }

        // 9) Mistakes should be cleared now
        var mistakesAfterResponse = await client.GetAsync("/api/scenes/2/mistakes");
        Assert.Equal(HttpStatusCode.OK, mistakesAfterResponse.StatusCode);

        var mistakesAfterJson = await mistakesAfterResponse.Content.ReadAsStringAsync();
        using (var mistakesAfterDoc = JsonDocument.Parse(mistakesAfterJson))
        {
            var root = mistakesAfterDoc.RootElement;

            AssertJsonHasAnyProperty(root, "sceneId", "id");
            AssertJsonHasProperties(root, "totalMistakes", "mistakeStepIds", "steps");

            Assert.Equal(2, GetSceneId(root));
            Assert.Equal(0, GetInt32PropertyIgnoreCase(root, "totalMistakes"));

            Assert.Equal(0, GetPropertyIgnoreCase(root, "mistakeStepIds").GetArrayLength());
            Assert.Equal(0, GetPropertyIgnoreCase(root, "steps").GetArrayLength());
        }

        // 10) For scenes with questions we MUST NOT call /api/scenes/complete (it returns 403).
        // Completion is marked by submit/mistakes-submit. Just verify /api/scenes/completed contains scene 2.

        var completedResponse = await client.GetAsync("/api/scenes/completed");
        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);

        var completedJson = await completedResponse.Content.ReadAsStringAsync();
        using var completedDoc = JsonDocument.Parse(completedJson);

        Assert.Equal(JsonValueKind.Array, completedDoc.RootElement.ValueKind);

        var ids = completedDoc.RootElement.EnumerateArray().Select(x => x.GetInt32()).ToList();

        Assert.Contains(2, ids);
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var p in element.EnumerateObject())
        {
            if (string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = p.Value;
                return true;
            }
        }

        return false;
    }

    private static JsonElement GetPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        if (TryGetPropertyIgnoreCase(element, propertyName, out var value))
        {
            return value;
        }

        Assert.Fail("Missing JSON property: " + propertyName);

        return default;
    }

    private static int GetInt32PropertyIgnoreCase(JsonElement element, string propertyName)
    {
        return GetPropertyIgnoreCase(element, propertyName).GetInt32();
    }

    private static bool GetBoolPropertyIgnoreCase(JsonElement element, string propertyName)
    {
        return GetPropertyIgnoreCase(element, propertyName).GetBoolean();
    }

    private static int GetSceneId(JsonElement element)
    {
        if (TryGetPropertyIgnoreCase(element, "sceneId", out var sceneId))
        {
            return sceneId.GetInt32();
        }

        if (TryGetPropertyIgnoreCase(element, "id", out var id))
        {
            return id.GetInt32();
        }

        Assert.Fail("Missing JSON property: sceneId or id");

        return 0;
    }

    private static void AssertJsonHasAnyProperty(JsonElement element, params string[] propertyNames)
    {
        Assert.True(propertyNames.Any(p => TryGetPropertyIgnoreCase(element, p, out _)),
            "Missing JSON property: " + string.Join(" or ", propertyNames));
    }

    private static void AssertJsonHasProperties(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (string.Equals(propertyName, "sceneId", StringComparison.Ordinal))
            {
                Assert.True(
                    TryGetPropertyIgnoreCase(element, "sceneId", out _) || TryGetPropertyIgnoreCase(element, "id", out _),
                    "Missing JSON property: sceneId");
                continue;
            }

            Assert.True(TryGetPropertyIgnoreCase(element, propertyName, out _), "Missing JSON property: " + propertyName);
        }
    }
}
