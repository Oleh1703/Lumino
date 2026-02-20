using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests.Integration.Http;

public class DtoContractsHttpIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public DtoContractsHttpIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LessonEndpoints_ShouldKeepDtoContract()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SeedBaseUserAndCourse(dbContext);
            SeedLessonWithExercises(dbContext, lessonId: 1, topicId: 1);
            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        // start course
        var startCourse = await client.PostAsync("/api/learning/courses/1/start", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, startCourse.StatusCode);

        // GET lesson by id
        var lessonResponse = await client.GetAsync("/api/lessons/1");
        Assert.Equal(HttpStatusCode.OK, lessonResponse.StatusCode);

        var lessonJson = await lessonResponse.Content.ReadAsStringAsync();
        using (var lessonDoc = JsonDocument.Parse(lessonJson))
        {
            var root = lessonDoc.RootElement;

            AssertJsonHasProperties(root, "id", "topicId", "title", "theory", "order");

            Assert.Equal(1, root.GetProperty("id").GetInt32());
            Assert.Equal(1, root.GetProperty("topicId").GetInt32());
        }

        // GET exercises for lesson
        var exercisesResponse = await client.GetAsync("/api/lessons/1/exercises");
        Assert.Equal(HttpStatusCode.OK, exercisesResponse.StatusCode);

        var exercisesJson = await exercisesResponse.Content.ReadAsStringAsync();
        using (var exercisesDoc = JsonDocument.Parse(exercisesJson))
        {
            var root = exercisesDoc.RootElement;

            Assert.Equal(JsonValueKind.Array, root.ValueKind);
            Assert.True(root.GetArrayLength() > 0);

            AssertJsonHasProperties(root[0], "id", "type", "question", "data", "order");
        }

        // submit lesson with one mistake (id=1 wrong, id=2 correct) => not passed
        var submitLessonRequest = new
        {
            lessonId = 1,
            idempotencyKey = "dto-contract-lesson-1",
            answers = new[]
            {
                new { exerciseId = 1, answer = "wrong" },
                new { exerciseId = 2, answer = "a" }
            }
        };

        var submitLesson = await client.PostAsync("/api/lesson-submit", ToJson(submitLessonRequest));
        Assert.Equal(HttpStatusCode.OK, submitLesson.StatusCode);

        var submitLessonJson = await submitLesson.Content.ReadAsStringAsync();
        using (var submitLessonDoc = JsonDocument.Parse(submitLessonJson))
        {
            var root = submitLessonDoc.RootElement;

            AssertJsonHasProperties(root, "totalExercises", "correctAnswers", "isPassed", "mistakeExerciseIds", "answers");

            var mistakes = root.GetProperty("mistakeExerciseIds");
            Assert.Equal(JsonValueKind.Array, mistakes.ValueKind);
            Assert.True(mistakes.GetArrayLength() > 0);

            var answers = root.GetProperty("answers");
            Assert.Equal(JsonValueKind.Array, answers.ValueKind);
            Assert.True(answers.GetArrayLength() > 0);

            AssertJsonHasProperties(answers[0], "exerciseId", "userAnswer", "correctAnswer", "isCorrect");
        }

        // GET mistakes for lesson
        var lessonMistakes = await client.GetAsync("/api/lessons/1/mistakes");
        Assert.Equal(HttpStatusCode.OK, lessonMistakes.StatusCode);

        var lessonMistakesJson = await lessonMistakes.Content.ReadAsStringAsync();
        using (var mistakesDoc = JsonDocument.Parse(lessonMistakesJson))
        {
            var root = mistakesDoc.RootElement;

            AssertJsonHasProperties(root, "lessonId", "totalMistakes", "mistakeExerciseIds", "exercises");

            Assert.Equal(1, root.GetProperty("lessonId").GetInt32());

            var exercises = root.GetProperty("exercises");
            Assert.Equal(JsonValueKind.Array, exercises.ValueKind);
            Assert.True(exercises.GetArrayLength() > 0);

            AssertJsonHasProperties(exercises[0], "id", "type", "question", "data", "order");
        }

        // submit mistakes (correct now)
        var submitLessonMistakesRequest = new
        {
            answers = new[]
            {
                new { exerciseId = 1, answer = "a" }
            }
        };

        var submitLessonMistakes = await client.PostAsync("/api/lessons/1/mistakes/submit", ToJson(submitLessonMistakesRequest));
        Assert.Equal(HttpStatusCode.OK, submitLessonMistakes.StatusCode);

        var submitLessonMistakesJson = await submitLessonMistakes.Content.ReadAsStringAsync();
        using (var submitMistakesDoc = JsonDocument.Parse(submitLessonMistakesJson))
        {
            var root = submitMistakesDoc.RootElement;

            AssertJsonHasProperties(root, "lessonId", "totalExercises", "correctAnswers", "isCompleted", "mistakeExerciseIds", "answers");

            Assert.Equal(1, root.GetProperty("lessonId").GetInt32());
        }
    }

    [Fact]
    public async Task SceneEndpoints_ShouldKeepDtoContract()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LuminoDbContext>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SeedBaseUserAndCourse(dbContext);
            SeedLessonWithExercises(dbContext, lessonId: 1, topicId: 1);

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
                    Speaker = "B",
                    Text = "Pick B",
                    StepType = "Choice",
                    MediaUrl = null,
                    ChoicesJson = "[{\"text\":\"B\",\"isCorrect\":true},{\"text\":\"A\",\"isCorrect\":false}]"
                }
            );

            // scenes unlocking is controlled by IOptions<LearningSettings> (Lumino.Api.Utils)
            // default SceneUnlockEveryLessons is 1, so there is nothing to seed into DbContext.

            dbContext.SaveChanges();
        }

        var client = _factory.CreateClient();

        // start course
        var startCourse = await client.PostAsync("/api/learning/courses/1/start", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, startCourse.StatusCode);

        // pass lesson #1 to unlock scenes
        var submitLessonRequest = new
        {
            lessonId = 1,
            idempotencyKey = "dto-contract-scene-pass-lesson-1",
            answers = new[]
            {
                new { exerciseId = 1, answer = "a" },
                new { exerciseId = 2, answer = "a" }
            }
        };

        var submitLesson = await client.PostAsync("/api/lesson-submit", ToJson(submitLessonRequest));
        Assert.Equal(HttpStatusCode.OK, submitLesson.StatusCode);

        // GET scene details
        var sceneDetailsResponse = await client.GetAsync("/api/scenes/2");
        Assert.Equal(HttpStatusCode.OK, sceneDetailsResponse.StatusCode);

        var sceneDetailsJson = await sceneDetailsResponse.Content.ReadAsStringAsync();
        using (var detailsDoc = JsonDocument.Parse(sceneDetailsJson))
        {
            var root = detailsDoc.RootElement;

            AssertJsonHasProperties(root,
                "id", "courseId", "order", "title", "description", "sceneType",
                "backgroundUrl", "audioUrl",
                "isCompleted", "isUnlocked", "unlockReason",
                "passedLessons", "requiredPassedLessons");
        }

        // GET scene content (steps)
        var sceneContentResponse = await client.GetAsync("/api/scenes/2/content");
        Assert.Equal(HttpStatusCode.OK, sceneContentResponse.StatusCode);

        var sceneContentJson = await sceneContentResponse.Content.ReadAsStringAsync();
        using (var contentDoc = JsonDocument.Parse(sceneContentJson))
        {
            var root = contentDoc.RootElement;

            // SceneContentResponse has "id" (not "sceneId")
            AssertJsonHasProperties(root,
                "id", "courseId", "order", "title", "description", "sceneType",
                "backgroundUrl", "audioUrl",
                "isCompleted", "isUnlocked", "unlockReason",
                "passedLessons", "requiredPassedLessons",
                "steps");

            var steps = root.GetProperty("steps");
            Assert.Equal(JsonValueKind.Array, steps.ValueKind);
            Assert.True(steps.GetArrayLength() > 0);

            AssertJsonHasProperties(steps[0], "id", "order", "speaker", "text", "stepType", "mediaUrl", "choicesJson");
        }

        // submit scene with one mistake => not completed
        var submitSceneRequest = new
        {
            idempotencyKey = "dto-contract-scene-2",
            answers = new[]
            {
                new { stepId = 21, answer = "B" }, // wrong
                new { stepId = 22, answer = "B" }  // correct
            }
        };

        var submitScene = await client.PostAsync("/api/scenes/2/submit", ToJson(submitSceneRequest));
        Assert.Equal(HttpStatusCode.OK, submitScene.StatusCode);

        var submitSceneJson = await submitScene.Content.ReadAsStringAsync();
        using (var submitSceneDoc = JsonDocument.Parse(submitSceneJson))
        {
            var root = submitSceneDoc.RootElement;

            AssertJsonHasProperties(root, "sceneId", "totalQuestions", "correctAnswers", "isCompleted", "mistakeStepIds", "answers");

            var answers = root.GetProperty("answers");
            Assert.Equal(JsonValueKind.Array, answers.ValueKind);
            Assert.True(answers.GetArrayLength() > 0);

            AssertJsonHasProperties(answers[0], "stepId", "userAnswer", "correctAnswer", "isCorrect");
        }

        // GET mistakes
        var sceneMistakesResponse = await client.GetAsync("/api/scenes/2/mistakes");
        Assert.Equal(HttpStatusCode.OK, sceneMistakesResponse.StatusCode);

        var sceneMistakesJson = await sceneMistakesResponse.Content.ReadAsStringAsync();
        using (var mistakesDoc = JsonDocument.Parse(sceneMistakesJson))
        {
            var root = mistakesDoc.RootElement;

            AssertJsonHasProperties(root, "sceneId", "totalMistakes", "mistakeStepIds", "steps");

            var steps = root.GetProperty("steps");
            Assert.Equal(JsonValueKind.Array, steps.ValueKind);
            Assert.True(steps.GetArrayLength() > 0);

            AssertJsonHasProperties(steps[0], "id", "order", "speaker", "text", "stepType", "mediaUrl", "choicesJson");
        }

        // submit mistakes (correct now)
        var submitMistakesRequest = new
        {
            idempotencyKey = "dto-contract-scene-2-mistakes",
            answers = new[]
            {
                new { stepId = 21, answer = "A" }
            }
        };

        var submitMistakes = await client.PostAsync("/api/scenes/2/mistakes/submit", ToJson(submitMistakesRequest));
        Assert.Equal(HttpStatusCode.OK, submitMistakes.StatusCode);

        var submitMistakesJson = await submitMistakes.Content.ReadAsStringAsync();
        using (var submitMistakesDoc = JsonDocument.Parse(submitMistakesJson))
        {
            var root = submitMistakesDoc.RootElement;

            AssertJsonHasProperties(root, "sceneId", "totalQuestions", "correctAnswers", "isCompleted", "mistakeStepIds", "answers");
        }
    }

    private static void SeedBaseUserAndCourse(LuminoDbContext dbContext)
    {
        dbContext.Users.Add(new User
        {
            Id = 10,
            Email = "test@test.com",
            PasswordHash = "hash",
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
    }

    private static void SeedLessonWithExercises(LuminoDbContext dbContext, int lessonId, int topicId)
    {
        dbContext.Lessons.Add(new Lesson
        {
            Id = lessonId,
            TopicId = topicId,
            Title = "Lesson " + lessonId,
            Theory = "",
            Order = 1
        });

        dbContext.Exercises.AddRange(
            new Exercise
            {
                Id = 1,
                LessonId = lessonId,
                Type = ExerciseType.Input,
                Question = "Q1",
                Data = "",
                CorrectAnswer = "a",
                Order = 1
            },
            new Exercise
            {
                Id = 2,
                LessonId = lessonId,
                Type = ExerciseType.Input,
                Question = "Q2",
                Data = "",
                CorrectAnswer = "a",
                Order = 2
            }
        );
    }

    private static StringContent ToJson(object body)
    {
        return new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
    }

    private static void AssertJsonHasProperties(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            Assert.True(element.TryGetProperty(propertyName, out _), "Missing JSON property: " + propertyName);
        }
    }
}
