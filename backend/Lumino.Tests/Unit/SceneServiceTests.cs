using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Services;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lumino.Tests;

public class SceneServiceTests
{
    [Fact]
    public void MarkCompleted_FirstTime_ShouldCreateAttempt_AndCreateProgress_AndIncreaseTotalScore()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "scene@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        // 2 результати одного lesson -> береться Max(score)
        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 10,
            Score = 2,
            TotalQuestions = 5,
            CompletedAt = DateTime.UtcNow
        });

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 2,
            UserId = 1,
            LessonId = 10,
            Score = 3,
            TotalQuestions = 5,
            CompletedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var settings = Options.Create(new LearningSettings
        {
            SceneCompletionScore = 5,
            SceneUnlockEveryLessons = 1
        });

        var service = new SceneService(dbContext, dateTimeProvider, achievementService, settings);

        service.MarkCompleted(userId: 1, sceneId: 1);

        var attempts = dbContext.SceneAttempts.ToList();
        Assert.Single(attempts);

        Assert.Equal(1, attempts[0].UserId);
        Assert.Equal(1, attempts[0].SceneId);
        Assert.True(attempts[0].IsCompleted);
        Assert.Equal(now, attempts[0].CompletedAt);

        var progress = dbContext.UserProgresses.FirstOrDefault(x => x.UserId == 1);
        Assert.NotNull(progress);

        // lessonsScore = Max(2,3)=3
        // scenesScore = 1 * 5 = 5
        Assert.Equal(8, progress!.TotalScore);
        Assert.Equal(0, progress.CompletedLessons);
        Assert.Equal(now, progress.LastUpdatedAt);

        Assert.Equal(1, achievementService.SceneChecksCount);
    }

    [Fact]
    public void MarkCompleted_WhenSceneLocked_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "locked@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        // scene 2 -> requiredLessons = (2-1)*1 = 1
        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 2,
            Title = "Scene 2",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 11, 0, 0, DateTimeKind.Utc);

        var service = new SceneService(
            dbContext,
            new FixedDateTimeProvider(now),
            new FakeAchievementService(),
            Options.Create(new LearningSettings { SceneCompletionScore = 5, SceneUnlockEveryLessons = 1 })
        );

        Assert.Throws<ForbiddenAccessException>(() =>
        {
            service.MarkCompleted(userId: 1, sceneId: 2);
        });
    }

    
    [Fact]
    public void MarkCompleted_WhenSceneHasQuestions_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "questions@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Choose",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"Cat\",\"isCorrect\":true},{\"text\":\"Dog\",\"isCorrect\":false}]"
        });

        dbContext.SaveChanges();

        var service = new SceneService(
            dbContext,
            new FixedDateTimeProvider(new DateTime(2026, 2, 12, 11, 30, 0, DateTimeKind.Utc)),
            new FakeAchievementService(),
            Options.Create(new LearningSettings { SceneCompletionScore = 5, SceneUnlockEveryLessons = 1 })
        );

        Assert.Throws<ForbiddenAccessException>(() =>
        {
            service.MarkCompleted(userId: 1, sceneId: 1);
        });
    }

[Fact]
    public void MarkCompleted_SecondTime_ShouldBeIdempotent_AndNotIncreaseScoreAgain()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "scene2@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneAttempts.Add(new SceneAttempt
        {
            Id = 1,
            UserId = 1,
            SceneId = 1,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow,
            Score = 0,
            TotalQuestions = 0,
            DetailsJson = null
        });

        dbContext.UserProgresses.Add(new UserProgress
        {
            Id = 1,
            UserId = 1,
            CompletedLessons = 0,
            TotalScore = 5,
            LastUpdatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 12, 0, 0, DateTimeKind.Utc);

        var achievementService = new CountingAchievementService();

        var service = new SceneService(
            dbContext,
            new FixedDateTimeProvider(now),
            achievementService,
            Options.Create(new LearningSettings { SceneCompletionScore = 5, SceneUnlockEveryLessons = 1 })
        );

        service.MarkCompleted(userId: 1, sceneId: 1);

        var attempts = dbContext.SceneAttempts.ToList();
        Assert.Single(attempts);

        var progress = dbContext.UserProgresses.FirstOrDefault(x => x.UserId == 1);
        Assert.NotNull(progress);

        // не змінилось вдруге
        Assert.Equal(5, progress!.TotalScore);

        Assert.Equal(0, achievementService.SceneChecksCount);
    }

    [Fact]
    public void GetSceneContent_WhenUnlocked_ShouldReturnSteps()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "content@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        // scene 2 unlocked якщо пройдено 1 lesson
        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 2,
            Title = "Scene 2",
            Description = "Desc",
            SceneType = "intro"
        });

        // lesson result щоб passedLessons = 1 (passingScorePercent = 80)
        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 10,
            Score = 4,
            TotalQuestions = 5,
            CompletedAt = DateTime.UtcNow
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 2,
            Order = 1,
            Speaker = "A",
            Text = "First",
            StepType = "Line",
            MediaUrl = null,
            ChoicesJson = null
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 2,
            SceneId = 2,
            Order = 2,
            Speaker = "B",
            Text = "Second",
            StepType = "Line",
            MediaUrl = null,
            ChoicesJson = null
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 17, 0, 0, DateTimeKind.Utc);

        var service = new SceneService(
            dbContext,
            new FixedDateTimeProvider(now),
            new FakeAchievementService(),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1 })
        );

        var result = service.GetSceneContent(userId: 1, sceneId: 2);

        Assert.NotNull(result);
        Assert.True(result!.IsUnlocked);
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(1, result.Steps[0].Order);
        Assert.Equal("First", result.Steps[0].Text);
        Assert.Equal(2, result.Steps[1].Order);
        Assert.Equal("Second", result.Steps[1].Text);
    }

    [Fact]
    public void SubmitScene_WhenAllAnswersCorrect_ShouldCompleteScene_AndSaveAttemptDetails()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "submitscene@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Choose",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"Cat\",\"isCorrect\":true},{\"text\":\"Dog\",\"isCorrect\":false}]"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 18, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var service = new SceneService(
            dbContext,
            dateTimeProvider,
            achievementService,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var result = service.SubmitScene(
            userId: 1,
            sceneId: 1,
            request: new SubmitSceneRequest
            {
                Answers = new List<SubmitSceneAnswerRequest>
                {
                    new SubmitSceneAnswerRequest { StepId = 1, Answer = "Cat" }
                }
            }
        );

        Assert.NotNull(result);
        Assert.Equal(1, result.SceneId);
        Assert.Equal(1, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.True(result.IsCompleted);
        Assert.Empty(result.MistakeStepIds);
        Assert.Single(result.Answers);
        Assert.True(result.Answers[0].IsCorrect);

        var attempt = dbContext.SceneAttempts.Single(x => x.UserId == 1 && x.SceneId == 1);

        Assert.True(attempt.IsCompleted);
        Assert.Equal(now, attempt.CompletedAt);
        Assert.Equal(1, attempt.Score);
        Assert.Equal(1, attempt.TotalQuestions);
        Assert.False(string.IsNullOrWhiteSpace(attempt.DetailsJson));

        Assert.Equal(1, achievementService.SceneChecksCount);
    }

    [Fact]
    public void SubmitScene_WhenInputAnswerIsAcceptable_ShouldCompleteScene()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "submitsceneinput@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        // Input step: correctAnswer + acceptableAnswers
        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Type the destination",
            StepType = "Input",
            MediaUrl = null,
            ChoicesJson = "{\"correctAnswer\":\"Paris\",\"acceptableAnswers\":[\"to paris\"]}"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 18, 5, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var service = new SceneService(
            dbContext,
            dateTimeProvider,
            achievementService,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var result = service.SubmitScene(
            userId: 1,
            sceneId: 1,
            request: new SubmitSceneRequest
            {
                Answers = new List<SubmitSceneAnswerRequest>
                {
                    new SubmitSceneAnswerRequest { StepId = 1, Answer = "to paris" }
                }
            }
        );

        Assert.NotNull(result);
        Assert.Equal(1, result.SceneId);
        Assert.Equal(1, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.True(result.IsCompleted);
        Assert.Empty(result.MistakeStepIds);
        Assert.Single(result.Answers);
        Assert.True(result.Answers[0].IsCorrect);

        var attempt = dbContext.SceneAttempts.Single(x => x.UserId == 1 && x.SceneId == 1);

        Assert.True(attempt.IsCompleted);
        Assert.Equal(now, attempt.CompletedAt);
        Assert.Equal(1, attempt.Score);
        Assert.Equal(1, attempt.TotalQuestions);

        Assert.Equal(1, achievementService.SceneChecksCount);
    }

    [Fact]
    public void SubmitScene_WhenChoiceAndInputCorrect_ShouldCompleteScene()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "submitscenemixed@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Choose",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"Cat\",\"isCorrect\":true},{\"text\":\"Dog\",\"isCorrect\":false}]"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 2,
            SceneId = 1,
            Order = 2,
            Speaker = "B",
            Text = "Type the destination",
            StepType = "Input",
            MediaUrl = null,
            ChoicesJson = "{\"correctAnswer\":\"Paris\",\"acceptableAnswers\":[\"to paris\"]}"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 18, 30, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var service = new SceneService(
            dbContext,
            dateTimeProvider,
            achievementService,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var result = service.SubmitScene(
            userId: 1,
            sceneId: 1,
            request: new SubmitSceneRequest
            {
                Answers = new List<SubmitSceneAnswerRequest>
                {
                    new SubmitSceneAnswerRequest { StepId = 1, Answer = "Cat" },
                    new SubmitSceneAnswerRequest { StepId = 2, Answer = "PARIS" }
                }
            }
        );

        Assert.NotNull(result);
        Assert.Equal(1, result.SceneId);
        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(2, result.CorrectAnswers);
        Assert.True(result.IsCompleted);
        Assert.Empty(result.MistakeStepIds);
        Assert.Equal(2, result.Answers.Count);
        Assert.All(result.Answers, x => Assert.True(x.IsCorrect));

        var attempt = dbContext.SceneAttempts.Single(x => x.UserId == 1 && x.SceneId == 1);

        Assert.True(attempt.IsCompleted);
        Assert.Equal(now, attempt.CompletedAt);
        Assert.Equal(2, attempt.Score);
        Assert.Equal(2, attempt.TotalQuestions);

        Assert.Equal(1, achievementService.SceneChecksCount);
    }

    [Fact]
    public void SubmitScene_WhenChoiceCorrectAndInputWrong_ShouldNotCompleteScene_AndReturnMistakeStep()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "submitscenemixedwrong@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Choose",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"Cat\",\"isCorrect\":true},{\"text\":\"Dog\",\"isCorrect\":false}]"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 2,
            SceneId = 1,
            Order = 2,
            Speaker = "B",
            Text = "Type the destination",
            StepType = "Input",
            MediaUrl = null,
            ChoicesJson = "{\"correctAnswer\":\"Paris\",\"acceptableAnswers\":[\"to paris\"]}"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 18, 40, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var service = new SceneService(
            dbContext,
            dateTimeProvider,
            achievementService,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var result = service.SubmitScene(
            userId: 1,
            sceneId: 1,
            request: new SubmitSceneRequest
            {
                Answers = new List<SubmitSceneAnswerRequest>
                {
                    new SubmitSceneAnswerRequest { StepId = 1, Answer = "Cat" },
                    new SubmitSceneAnswerRequest { StepId = 2, Answer = "London" }
                }
            }
        );

        Assert.NotNull(result);
        Assert.Equal(1, result.SceneId);
        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.False(result.IsCompleted);

        Assert.Single(result.MistakeStepIds);
        Assert.Contains(2, result.MistakeStepIds);

        Assert.Equal(2, result.Answers.Count);
        Assert.True(result.Answers.First(x => x.StepId == 1).IsCorrect);
        Assert.False(result.Answers.First(x => x.StepId == 2).IsCorrect);

        var attempt = dbContext.SceneAttempts.Single(x => x.UserId == 1 && x.SceneId == 1);

        Assert.False(attempt.IsCompleted);
        Assert.Equal(1, attempt.Score);
        Assert.Equal(2, attempt.TotalQuestions);
        Assert.Equal(now, attempt.CompletedAt);
        Assert.False(string.IsNullOrWhiteSpace(attempt.DetailsJson));

        Assert.Equal(0, achievementService.SceneChecksCount);
    }

    

    [Fact]
    public void SubmitScene_WhenPassingPercent50_ShouldCompleteSceneButKeepMistakes()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "scenepassing50@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Choose",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"Cat\",\"isCorrect\":true},{\"text\":\"Dog\",\"isCorrect\":false}]"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 2,
            SceneId = 1,
            Order = 2,
            Speaker = "B",
            Text = "Type the destination",
            StepType = "Input",
            MediaUrl = null,
            ChoicesJson = "{\"correctAnswer\":\"Paris\",\"acceptableAnswers\":[\"to paris\"]}"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 18, 41, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var service = new SceneService(
            dbContext,
            dateTimeProvider,
            achievementService,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5, ScenePassingPercent = 50 })
        );

        var result = service.SubmitScene(
            userId: 1,
            sceneId: 1,
            request: new SubmitSceneRequest
            {
                Answers = new List<SubmitSceneAnswerRequest>
                {
                    new SubmitSceneAnswerRequest { StepId = 1, Answer = "Cat" },
                    new SubmitSceneAnswerRequest { StepId = 2, Answer = "London" }
                }
            }
        );

        Assert.NotNull(result);
        Assert.Equal(1, result.SceneId);
        Assert.Equal(2, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.True(result.IsCompleted);

        Assert.Single(result.MistakeStepIds);
        Assert.Contains(2, result.MistakeStepIds);

        var attempt = dbContext.SceneAttempts.Single(x => x.UserId == 1 && x.SceneId == 1);

        Assert.True(attempt.IsCompleted);
        Assert.Equal(1, attempt.Score);
        Assert.Equal(2, attempt.TotalQuestions);
        Assert.Equal(now, attempt.CompletedAt);
        Assert.False(string.IsNullOrWhiteSpace(attempt.DetailsJson));

        Assert.Equal(1, achievementService.SceneChecksCount);
    }

    [Fact]
    public void GetSceneMistakes_WhenHasMistakes_ShouldReturnOnlyMistakeSteps()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "mistakes@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Choose",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"Cat\",\"isCorrect\":true},{\"text\":\"Dog\",\"isCorrect\":false}]"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 2,
            SceneId = 1,
            Order = 2,
            Speaker = "B",
            Text = "Type the destination",
            StepType = "Input",
            MediaUrl = null,
            ChoicesJson = "{\"correctAnswer\":\"Paris\",\"acceptableAnswers\":[\"to paris\"]}"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 19, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var service = new SceneService(
            dbContext,
            dateTimeProvider,
            achievementService,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        // робимо спробу з 1 помилкою (Input)
        var submit = service.SubmitScene(
            userId: 1,
            sceneId: 1,
            request: new SubmitSceneRequest
            {
                Answers = new List<SubmitSceneAnswerRequest>
                {
                    new SubmitSceneAnswerRequest { StepId = 1, Answer = "Cat" },
                    new SubmitSceneAnswerRequest { StepId = 2, Answer = "London" }
                }
            }
        );

        Assert.NotNull(submit);
        Assert.False(submit.IsCompleted);
        Assert.Single(submit.MistakeStepIds);
        Assert.Contains(2, submit.MistakeStepIds);

        var mistakes = service.GetSceneMistakes(userId: 1, sceneId: 1);

        Assert.NotNull(mistakes);
        Assert.Equal(1, mistakes.SceneId);
        Assert.Equal(1, mistakes.TotalMistakes);
        Assert.Single(mistakes.MistakeStepIds);
        Assert.Contains(2, mistakes.MistakeStepIds);

        Assert.Single(mistakes.Steps);
        Assert.Equal(2, mistakes.Steps[0].Id);
        Assert.Equal(2, mistakes.Steps[0].Order);
        Assert.Equal("Input", mistakes.Steps[0].StepType);
    }

    [Fact]
    public void SubmitSceneMistakes_WhenFixMistakes_ShouldCompleteScene()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "mistakesfix@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Choose",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"Cat\",\"isCorrect\":true},{\"text\":\"Dog\",\"isCorrect\":false}]"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 2,
            SceneId = 1,
            Order = 2,
            Speaker = "B",
            Text = "Type the destination",
            StepType = "Input",
            MediaUrl = null,
            ChoicesJson = "{\"correctAnswer\":\"Paris\",\"acceptableAnswers\":[\"to paris\"]}"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 19, 30, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var service = new SceneService(
            dbContext,
            dateTimeProvider,
            achievementService,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        // перший submit з помилкою на Input
        var first = service.SubmitScene(
            userId: 1,
            sceneId: 1,
            request: new SubmitSceneRequest
            {
                Answers = new List<SubmitSceneAnswerRequest>
                {
                    new SubmitSceneAnswerRequest { StepId = 1, Answer = "Cat" },
                    new SubmitSceneAnswerRequest { StepId = 2, Answer = "London" }
                }
            }
        );

        Assert.False(first.IsCompleted);
        Assert.Single(first.MistakeStepIds);
        Assert.Contains(2, first.MistakeStepIds);
        Assert.Equal(0, achievementService.SceneChecksCount);

        // submit тільки помилок
        var second = service.SubmitSceneMistakes(
            userId: 1,
            sceneId: 1,
            request: new SubmitSceneRequest
            {
                Answers = new List<SubmitSceneAnswerRequest>
                {
                    new SubmitSceneAnswerRequest { StepId = 2, Answer = "PARIS" }
                }
            }
        );

        Assert.True(second.IsCompleted);
        Assert.Equal(2, second.TotalQuestions);
        Assert.Equal(2, second.CorrectAnswers);
        Assert.Empty(second.MistakeStepIds);

        var attempt = dbContext.SceneAttempts.Single(x => x.UserId == 1 && x.SceneId == 1);

        Assert.True(attempt.IsCompleted);
        Assert.Equal(2, attempt.Score);
        Assert.Equal(2, attempt.TotalQuestions);
        Assert.Equal(now, attempt.CompletedAt);

        Assert.Equal(1, achievementService.SceneChecksCount);
    }

[Fact]
    public void SubmitScene_WhenAnswerIsWrong_ShouldNotCompleteScene_AndReturnMistake()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "submitscenewrong@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 1,
            Order = 1,
            Speaker = "A",
            Text = "Choose",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"Cat\",\"isCorrect\":true},{\"text\":\"Dog\",\"isCorrect\":false}]"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 18, 10, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new CountingAchievementService();

        var service = new SceneService(
            dbContext,
            dateTimeProvider,
            achievementService,
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1, SceneCompletionScore = 5 })
        );

        var result = service.SubmitScene(
            userId: 1,
            sceneId: 1,
            request: new SubmitSceneRequest
            {
                Answers = new List<SubmitSceneAnswerRequest>
                {
                    new SubmitSceneAnswerRequest { StepId = 1, Answer = "Dog" }
                }
            }
        );

        Assert.NotNull(result);
        Assert.Equal(1, result.SceneId);
        Assert.Equal(1, result.TotalQuestions);
        Assert.Equal(0, result.CorrectAnswers);
        Assert.False(result.IsCompleted);
        Assert.Single(result.MistakeStepIds);
        Assert.Contains(1, result.MistakeStepIds);

        var attempt = dbContext.SceneAttempts.Single(x => x.UserId == 1 && x.SceneId == 1);

        Assert.False(attempt.IsCompleted);
        Assert.Equal(0, attempt.Score);
        Assert.Equal(1, attempt.TotalQuestions);
        Assert.False(string.IsNullOrWhiteSpace(attempt.DetailsJson));

        Assert.Equal(0, achievementService.SceneChecksCount);
    }

    [Fact]
    public void SubmitScene_WhenSceneLocked_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "submitscenelocked@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        // scene 2 -> requiredLessons = (2-1)*1 = 1
        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 2,
            Title = "Scene 2",
            Description = "Desc",
            SceneType = "intro"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 1,
            SceneId = 2,
            Order = 1,
            Speaker = "A",
            Text = "Choose",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\":\"Yes\",\"isCorrect\":true},{\"text\":\"No\",\"isCorrect\":false}]"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 18, 20, 0, DateTimeKind.Utc);

        var service = new SceneService(
            dbContext,
            new FixedDateTimeProvider(now),
            new FakeAchievementService(),
            Options.Create(new LearningSettings { PassingScorePercent = 80, SceneUnlockEveryLessons = 1 })
        );

        Assert.Throws<ForbiddenAccessException>(() =>
        {
            service.SubmitScene(
                userId: 1,
                sceneId: 2,
                request: new SubmitSceneRequest
                {
                    Answers = new List<SubmitSceneAnswerRequest>
                    {
                        new SubmitSceneAnswerRequest { StepId = 1, Answer = "Yes" }
                    }
                }
            );
        });
    }



    [Fact]
    public void SubmitScene_WhenCompleted_ShouldAddVocabularyFromScene_AndScheduleMistakeWordsAsDueNow()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "vocab@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.VocabularyItems.Add(new VocabularyItem { Id = 1, Word = "coffee", Translation = "кава", Example = "Coffee, please." });
        dbContext.VocabularyItems.Add(new VocabularyItem { Id = 2, Word = "thank you", Translation = "дякую", Example = "Thank you!" });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Cafe",
            Description = "Desc",
            SceneType = "Dialog",
            Order = 1
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 10,
            SceneId = 1,
            Order = 1,
            Speaker = "Quiz",
            Text = "What did you order?",
            StepType = "Choice",
            MediaUrl = null,
            ChoicesJson = "[{\"text\": \"Coffee\", \"isCorrect\": true}, {\"text\": \"Tea\", \"isCorrect\": false}]"
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 11,
            SceneId = 1,
            Order = 2,
            Speaker = "Quiz",
            Text = "Type: Thank you",
            StepType = "Input",
            MediaUrl = null,
            ChoicesJson = "{\"correctAnswer\": \"Thank you\", \"acceptableAnswers\": [\"Thanks\"]}"
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 18, 40, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new FakeAchievementService();

        var settings = Options.Create(new LearningSettings
        {
            SceneCompletionScore = 5,
            SceneUnlockEveryLessons = 1,
            ScenePassingPercent = 50
        });

        var service = new SceneService(dbContext, dateTimeProvider, achievementService, settings);

        var response = service.SubmitScene(1, 1, new SubmitSceneRequest
        {
            Answers = new List<SubmitSceneAnswerRequest>
            {
                new SubmitSceneAnswerRequest { StepId = 10, Answer = "Tea" }, // wrong
                new SubmitSceneAnswerRequest { StepId = 11, Answer = "Thank you" } // correct
            }
        });

        Assert.True(response.IsCompleted);

        var userWords = dbContext.UserVocabularies
            .Where(x => x.UserId == 1)
            .ToList();

        Assert.Equal(2, userWords.Count);

        var coffee = userWords.First(x => x.VocabularyItemId == 1);
        var thanks = userWords.First(x => x.VocabularyItemId == 2);

        Assert.Equal(now, coffee.NextReviewAt); // mistake => due now
        Assert.Equal(now.AddDays(1), thanks.NextReviewAt); // ok => due tomorrow
    }

    [Fact]
    public void MarkCompleted_WhenNoQuestions_ShouldAddVocabularyFromLines()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "vocab2@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.VocabularyItems.Add(new VocabularyItem { Id = 1, Word = "hello", Translation = "привіт", Example = "Hello!" });
        dbContext.VocabularyItems.Add(new VocabularyItem { Id = 2, Word = "goodbye", Translation = "до побачення", Example = "Goodbye!" });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Intro",
            Description = "Desc",
            SceneType = "Dialog",
            Order = 1
        });

        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 10,
            SceneId = 1,
            Order = 1,
            Speaker = "Guide",
            Text = "Hello and goodbye!",
            StepType = "Line",
            MediaUrl = null,
            ChoicesJson = null
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 19, 0, 0, DateTimeKind.Utc);
        var dateTimeProvider = new FixedDateTimeProvider(now);

        var achievementService = new FakeAchievementService();

        var settings = Options.Create(new LearningSettings
        {
            SceneCompletionScore = 5,
            SceneUnlockEveryLessons = 1
        });

        var service = new SceneService(dbContext, dateTimeProvider, achievementService, settings);

        service.MarkCompleted(1, 1);

        var userWords = dbContext.UserVocabularies
            .Where(x => x.UserId == 1)
            .ToList();

        Assert.Equal(2, userWords.Count);
        Assert.Contains(userWords, x => x.VocabularyItemId == 1);
        Assert.Contains(userWords, x => x.VocabularyItemId == 2);
    }
    private class CountingAchievementService : IAchievementService
    {
        public int SceneChecksCount { get; private set; }

        public void CheckAndGrantAchievements(int userId, int lessonScore, int totalQuestions) { }

        public void CheckAndGrantSceneAchievements(int userId)
        {
            SceneChecksCount++;
        }
    }
    [Fact]
    public void SubmitScene_WhenSaveChangesThrowsDbUpdateExceptionOnFirstInsert_ShouldNotCrash()
    {
        var options = new DbContextOptionsBuilder<LuminoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new ThrowOnceOnSceneAttemptInsertDbContext(options);

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "scene@mail.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        dbContext.Scenes.Add(new Scene
        {
            Id = 1,
            Title = "Scene 1",
            Description = "Desc",
            SceneType = "intro",
            CourseId = null,
            Order = 1
        });

        // 1 питання Choice
        dbContext.SceneSteps.Add(new SceneStep
        {
            Id = 11,
            SceneId = 1,
            Order = 1,
            Speaker = "System",
            Text = "Q1",
            StepType = "Choice",
            ChoicesJson = "[{\"text\":\"A\",\"isCorrect\":true},{\"text\":\"B\",\"isCorrect\":false}]"
        });

        dbContext.SaveChanges();

        var service = new SceneService(
            dbContext,
            new FakeDateTimeProvider(),
            new FakeAchievementService(),
            Options.Create(new LearningSettings
            {
                PassingScorePercent = 80,
                SceneCompletionScore = 5,
                ScenePassingPercent = 100,
                DailyGoalScoreTarget = 20
            }));

        var result = service.SubmitScene(1, 1, new SubmitSceneRequest
        {
            IdempotencyKey = null,
            Answers =
            {
                new SubmitSceneAnswerRequest
                {
                    StepId = 11,
                    Answer = "A"
                }
            }
        });

        Assert.True(result.IsCompleted);

        var attempt = dbContext.SceneAttempts.FirstOrDefault(x => x.UserId == 1 && x.SceneId == 1);
        Assert.NotNull(attempt);
        Assert.True(attempt!.IsCompleted);
    }

    private class ThrowOnceOnSceneAttemptInsertDbContext : LuminoDbContext
    {
        private bool _hasThrown;

        public ThrowOnceOnSceneAttemptInsertDbContext(DbContextOptions<LuminoDbContext> options)
            : base(options)
        {
        }

        public override int SaveChanges()
        {
            // симулюємо ситуацію, коли інший запит “встиг” створити SceneAttempt,
            // і наша вставка ловить DbUpdateException на унікальному індексі UserId+SceneId
            if (!_hasThrown && ChangeTracker.Entries<SceneAttempt>().Any(x => x.State == EntityState.Added))
            {
                _hasThrown = true;

                // спочатку зберігаємо вставку, щоб запис реально існував у БД,
                // а потім кидаємо DbUpdateException — так ми детерміновано перевіряємо catch-гілку в сервісі
                base.SaveChanges();
                throw new DbUpdateException("Simulated concurrent insert");
            }

            return base.SaveChanges();
        }
    }
}