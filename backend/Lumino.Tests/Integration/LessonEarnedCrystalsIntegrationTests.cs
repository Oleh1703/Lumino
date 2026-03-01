using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Xunit;

namespace Lumino.Tests.Integration;

public class LessonEarnedCrystalsIntegrationTests
{
    [Fact]
    public void SubmitLesson_FirstPassed_ShouldReturnEarnedCrystals_And_SecondPassed_ShouldReturnZero()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "user@test.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            Role = Role.User,
            CreatedAt = DateTime.UtcNow,
            NativeLanguageCode = "uk",
            TargetLanguageCode = "en",
            Crystals = 0
        });

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            LanguageCode = "en",
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
            Theory = "Text",
            Order = 1
        });

        for (int i = 1; i <= 9; i++)
        {
            dbContext.Exercises.Add(new Exercise
            {
                Id = i,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q" + i,
                CorrectAnswer = "A" + i,
                Data = "",
                Order = i
            });
        }

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 1,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false,
            BestScore = 0
        });

        dbContext.SaveChanges();

        var settings = TestLearningSettingsFactory.Create(new LearningSettings
        {
            PassingScorePercent = 80,
            CrystalsRewardPerPassedLesson = 3
        });

        var dateTimeProvider = new FakeDateTimeProvider();
        var achievementService = new FakeAchievementService();
        var economyService = new UserEconomyService(dbContext, settings);
        var streakService = new StreakService(dbContext, dateTimeProvider);
        var validator = new SubmitLessonRequestValidator();

        var service = new LessonResultService(
            dbContext,
            achievementService,
            dateTimeProvider,
            economyService,
            streakService,
            validator,
            settings);

        var request = new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "A1" },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "A2" },
                new SubmitExerciseAnswerRequest { ExerciseId = 3, Answer = "A3" },
                new SubmitExerciseAnswerRequest { ExerciseId = 4, Answer = "A4" },
                new SubmitExerciseAnswerRequest { ExerciseId = 5, Answer = "A5" },
                new SubmitExerciseAnswerRequest { ExerciseId = 6, Answer = "A6" },
                new SubmitExerciseAnswerRequest { ExerciseId = 7, Answer = "A7" },
                new SubmitExerciseAnswerRequest { ExerciseId = 8, Answer = "A8" },
                new SubmitExerciseAnswerRequest { ExerciseId = 9, Answer = "A9" }
            }
        };

        var first = service.SubmitLesson(1, request);

        Assert.True(first.IsPassed);
        Assert.Equal(3, first.EarnedCrystals);

        var second = service.SubmitLesson(1, request);

        Assert.True(second.IsPassed);
        Assert.Equal(0, second.EarnedCrystals);
    }

    [Fact]
    public void SubmitLesson_WithSameIdempotencyKey_ShouldReturnSameEarnedCrystals()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.Add(new User
        {
            Id = 1,
            Email = "user@test.com",
            PasswordHash = "hash",
            IsEmailVerified = true,
            Role = Role.User,
            CreatedAt = DateTime.UtcNow,
            NativeLanguageCode = "uk",
            TargetLanguageCode = "en",
            Crystals = 0
        });

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            LanguageCode = "en",
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
            Theory = "Text",
            Order = 1
        });

        for (int i = 1; i <= 9; i++)
        {
            dbContext.Exercises.Add(new Exercise
            {
                Id = i,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q" + i,
                CorrectAnswer = "A" + i,
                Data = "",
                Order = i
            });
        }

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 1,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false,
            BestScore = 0
        });

        dbContext.SaveChanges();

        var settings = TestLearningSettingsFactory.Create(new LearningSettings
        {
            PassingScorePercent = 80,
            CrystalsRewardPerPassedLesson = 2
        });

        var dateTimeProvider = new FakeDateTimeProvider();
        var achievementService = new FakeAchievementService();
        var economyService = new UserEconomyService(dbContext, settings);
        var streakService = new StreakService(dbContext, dateTimeProvider);
        var validator = new SubmitLessonRequestValidator();

        var service = new LessonResultService(
            dbContext,
            achievementService,
            dateTimeProvider,
            economyService,
            streakService,
            validator,
            settings);

        var request = new SubmitLessonRequest
        {
            LessonId = 1,
            IdempotencyKey = "same-key",
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "A1" },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "A2" },
                new SubmitExerciseAnswerRequest { ExerciseId = 3, Answer = "A3" },
                new SubmitExerciseAnswerRequest { ExerciseId = 4, Answer = "A4" },
                new SubmitExerciseAnswerRequest { ExerciseId = 5, Answer = "A5" },
                new SubmitExerciseAnswerRequest { ExerciseId = 6, Answer = "A6" },
                new SubmitExerciseAnswerRequest { ExerciseId = 7, Answer = "A7" },
                new SubmitExerciseAnswerRequest { ExerciseId = 8, Answer = "A8" },
                new SubmitExerciseAnswerRequest { ExerciseId = 9, Answer = "A9" }
            }
        };

        var first = service.SubmitLesson(1, request);
        var second = service.SubmitLesson(1, request);

        Assert.True(first.IsPassed);
        Assert.Equal(2, first.EarnedCrystals);

        Assert.True(second.IsPassed);
        Assert.Equal(2, second.EarnedCrystals);
    }
}
