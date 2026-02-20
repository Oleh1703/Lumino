using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests.Integration;

public class SceneHappyPathIntegrationTests
{
    [Fact]
    public void StartCourse_PassLesson_Then_Next_ShouldReturnUnlockedScene_Then_SubmitSceneCompleted_Then_NextNull()
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

        // Scenes in this course:
        // Scene #1 - already completed (so Next should skip it)
        // Scene #2 - requires 1 passed lesson when SceneUnlockEveryLessons = 1
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
            SceneUnlockEveryLessons = 1
        });

        var courseProgressService = new CourseProgressService(dbContext, dateTimeProvider, settings);

        var lessonResultService = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            dateTimeProvider,
            new SubmitLessonRequestValidator(),
            settings
        );

        var sceneService = new SceneService(
            dbContext,
            dateTimeProvider,
            new FakeAchievementService(),
            settings
        );

        var nextActivityService = new NextActivityService(dbContext, dateTimeProvider, settings);

        // 1) start course -> unlock first lesson
        courseProgressService.StartCourse(10, 1);

        // 2) pass lesson 1 (100%)
        var lessonSubmit = lessonResultService.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "a" }
            }
        });

        Assert.True(lessonSubmit.IsPassed);

        // 3) Next -> should return Scene 2 (scene 1 already completed, scene 2 unlocked after 1 passed lesson)
        var next1 = nextActivityService.GetNext(10);

        Assert.NotNull(next1);
        Assert.Equal("Scene", next1!.Type);
        Assert.Equal(2, next1.SceneId);

        // 4) submit scene 2 with correct answer
        var sceneSubmit = sceneService.SubmitScene(10, 2, new SubmitSceneRequest
        {
            Answers = new List<SubmitSceneAnswerRequest>
            {
                new SubmitSceneAnswerRequest { StepId = 1, Answer = "A" }
            }
        });

        Assert.True(sceneSubmit.IsCompleted);
        Assert.Equal(1, sceneSubmit.TotalQuestions);
        Assert.Equal(1, sceneSubmit.CorrectAnswers);

        // 5) Next -> no lessons/vocab/scenes left
        var next2 = nextActivityService.GetNext(10);

        Assert.NotNull(next2);
        Assert.Equal("CourseComplete", next2!.Type);
        Assert.Equal(1, next2.CourseId);
        Assert.False(next2.IsLocked);
        Assert.Null(next2.LessonId);
        Assert.Null(next2.SceneId);
        Assert.Null(next2.UserVocabularyId);
    }
}
