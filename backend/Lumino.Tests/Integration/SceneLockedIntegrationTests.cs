using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests.Integration;

public class SceneLockedIntegrationTests
{
    [Fact]
    public void StartCourse_PassOneLesson_WhenSecondSceneRequiresTwoLessons_NextShouldNotReturnSceneAndSubmitThrowsForbidden()
    {
        var now = new DateTime(2026, 02, 16, 12, 0, 0, DateTimeKind.Utc);

        var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new FixedDateTimeProvider(now);

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

        // Only one lesson exists. User will pass it -> no lessons left.
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

        // Two scenes in course:
        // Scene 1 (Order=1) is already completed by user (skip).
        // Scene 2 (Order=2) requires 2 passed lessons when SceneUnlockEveryLessons = 2 -> locked after only 1 lesson passed.
        dbContext.Scenes.AddRange(
            new Scene
            {
                Id = 1,
                CourseId = 1,
                Order = 1,
                Title = "Scene 1",
                Description = "",
                SceneType = "Dialogue",
                BackgroundUrl = null,
                AudioUrl = null
            },
            new Scene
            {
                Id = 2,
                CourseId = 1,
                Order = 2,
                Title = "Scene 2",
                Description = "",
                SceneType = "Quiz",
                BackgroundUrl = null,
                AudioUrl = null
            }
        );

        dbContext.SceneAttempts.Add(new SceneAttempt
        {
            UserId = 10,
            SceneId = 1,
            IsCompleted = true,
            CompletedAt = now,
            Score = 0,
            TotalQuestions = 0,
            DetailsJson = null
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 2,
            Order = 1,
            Speaker = "NPC",
            Text = "Choose correct answer",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"A\",\"isCorrect\":true},{\"text\":\"B\",\"isCorrect\":false}]"
        });

        dbContext.SaveChanges();

        var settings = Options.Create(new LearningSettings
        {
            PassingScorePercent = 80,
            SceneUnlockEveryLessons = 2
        });

        var courseProgressService = new CourseProgressService(dbContext, dateTimeProvider, settings);

        var lessonResultService = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            dateTimeProvider,
            new SubmitLessonRequestValidator(),
            settings
        );

        var nextActivityService = new NextActivityService(dbContext, dateTimeProvider, settings);

        var sceneService = new SceneService(
            dbContext,
            dateTimeProvider,
            new FakeAchievementService(),
            settings
        );

        // 1) start course -> unlock first lesson
        courseProgressService.StartCourse(10, 1);

        // 2) pass the only lesson (100%)
        var lessonSubmit = lessonResultService.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "a" }
            }
        });

        Assert.True(lessonSubmit.IsPassed);

        // 3) Next -> should not return Scene (scene #2 locked, scene #1 completed, lessons/vocab отсутствуют)
        var next = nextActivityService.GetNext(10);

        Assert.NotNull(next);
        Assert.Equal("CourseComplete", next!.Type);
        Assert.Equal(1, next.CourseId);
        Assert.False(next.IsLocked);
        Assert.Null(next.LessonId);
        Assert.Null(next.SceneId);
        Assert.Null(next.UserVocabularyId);

        // 4) direct submit to locked scene must throw Forbidden
        Assert.Throws<ForbiddenAccessException>(() => sceneService.SubmitScene(10, 2, new SubmitSceneRequest
        {
            Answers = new List<SubmitSceneAnswerRequest>
            {
                new SubmitSceneAnswerRequest { StepId = 1, Answer = "A" }
            }
        }));
    }
}
