using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Lumino.Tests;

public class LessonMistakesServiceTests
{
    [Fact]
    public void GetLessonMistakes_ShouldReturnMistakeExercises_FromLastResult()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedLessonBase(dbContext, lessonId: 1, isUnlocked: true);

        dbContext.Exercises.Add(new Exercise
        {
            Id = 10,
            LessonId = 1,
            Order = 1,
            Type = ExerciseType.Input,
            Question = "Translate",
            CorrectAnswer = "hello",
            Data = "{}"
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 11,
            LessonId = 1,
            Order = 2,
            Type = ExerciseType.Input,
            Question = "Translate",
            CorrectAnswer = "world",
            Data = "{}"
        });

        var details = new LessonResultDetailsJson
        {
            MistakeExerciseIds = new() { 11 },
            Answers = new()
            {
                new LessonAnswerResultDto { ExerciseId = 10, UserAnswer = "hello", CorrectAnswer = "hello", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 11, UserAnswer = "", CorrectAnswer = "world", IsCorrect = false }
            }
        };

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 1,
            Score = 1,
            TotalQuestions = 2,
            CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
            MistakesJson = JsonSerializer.Serialize(details)
        });

        dbContext.SaveChanges();

        var service = CreateService(dbContext);

        var result = service.GetLessonMistakes(userId: 1, lessonId: 1);

        Assert.Equal(1, result.LessonId);
        Assert.Equal(1, result.TotalMistakes);
        Assert.Single(result.MistakeExerciseIds);
        Assert.Equal(11, result.MistakeExerciseIds[0]);

        Assert.Single(result.Exercises);
        Assert.Equal(11, result.Exercises[0].Id);
    }

    [Fact]
    public void SubmitLessonMistakes_WhenAnswerCorrect_ShouldClearMistakes_AndUpdateResult()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedLessonBase(dbContext, lessonId: 1, isUnlocked: true);

        dbContext.Exercises.Add(new Exercise
        {
            Id = 10,
            LessonId = 1,
            Order = 1,
            Type = ExerciseType.Input,
            Question = "Translate",
            CorrectAnswer = "hello",
            Data = "{}"
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 11,
            LessonId = 1,
            Order = 2,
            Type = ExerciseType.Input,
            Question = "Translate",
            CorrectAnswer = "world",
            Data = "{}"
        });

        var details = new LessonResultDetailsJson
        {
            MistakeExerciseIds = new() { 11 },
            Answers = new()
            {
                new LessonAnswerResultDto { ExerciseId = 10, UserAnswer = "hello", CorrectAnswer = "hello", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 11, UserAnswer = "WRONG", CorrectAnswer = "world", IsCorrect = false }
            }
        };

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 1,
            Score = 1,
            TotalQuestions = 2,
            CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
            MistakesJson = JsonSerializer.Serialize(details)
        });

        dbContext.SaveChanges();

        var service = CreateService(dbContext);

        var response = service.SubmitLessonMistakes(userId: 1, lessonId: 1, new SubmitLessonMistakesRequest
        {
            Answers = new()
            {
                new SubmitExerciseAnswerRequest
                {
                    ExerciseId = 11,
                    Answer = "world"
                }
            }
        });

        Assert.Equal(1, response.LessonId);
        Assert.True(response.IsCompleted);
        Assert.Empty(response.MistakeExerciseIds);
        Assert.Equal(2, response.TotalExercises);
        Assert.Equal(2, response.CorrectAnswers);

        var saved = dbContext.LessonResults.First(x => x.Id == 1);

        Assert.Equal(2, saved.Score);
        Assert.Equal(2, saved.TotalQuestions);
        Assert.False(string.IsNullOrWhiteSpace(saved.MistakesJson));

        var updated = JsonSerializer.Deserialize<LessonResultDetailsJson>(saved.MistakesJson!, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(updated);
        Assert.Empty(updated!.MistakeExerciseIds);
        Assert.Equal(2, updated.Answers.Count);
        Assert.True(updated.Answers.First(x => x.ExerciseId == 11).IsCorrect);
    }

    [Fact]
    public void GetLessonMistakes_WhenLessonLocked_ShouldThrowForbiddenAccessException()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedLessonBase(dbContext, lessonId: 1, isUnlocked: false);

        var service = CreateService(dbContext);

        Assert.Throws<ForbiddenAccessException>(() => service.GetLessonMistakes(userId: 1, lessonId: 1));
    }

    [Fact]
    public void GetLessonMistakes_WhenMistakesJsonInvalid_ShouldReturnEmptyMistakes()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedLessonBase(dbContext, lessonId: 1, isUnlocked: true);

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 1,
            Score = 0,
            TotalQuestions = 0,
            CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
            MistakesJson = "{ invalid json"
        });

        dbContext.SaveChanges();

        var service = CreateService(dbContext);

        var result = service.GetLessonMistakes(userId: 1, lessonId: 1);

        Assert.Equal(1, result.LessonId);
        Assert.Equal(0, result.TotalMistakes);
        Assert.Empty(result.MistakeExerciseIds);
        Assert.Empty(result.Exercises);
    }

    [Fact]
    public void SubmitLessonMistakes_WhenDuplicateExerciseIdsInRequest_ShouldThrowArgumentException()
    {
        var dbContext = TestDbContextFactory.Create();

        SeedLessonBase(dbContext, lessonId: 1, isUnlocked: true);

        dbContext.Exercises.Add(new Exercise
        {
            Id = 11,
            LessonId = 1,
            Order = 1,
            Type = ExerciseType.Input,
            Question = "Translate",
            CorrectAnswer = "world",
            Data = "{}"
        });

        var details = new LessonResultDetailsJson
        {
            MistakeExerciseIds = new() { 11 },
            Answers = new()
            {
                new LessonAnswerResultDto { ExerciseId = 11, UserAnswer = "WRONG", CorrectAnswer = "world", IsCorrect = false }
            }
        };

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 1,
            Score = 0,
            TotalQuestions = 1,
            CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
            MistakesJson = JsonSerializer.Serialize(details)
        });

        dbContext.SaveChanges();

        var service = CreateService(dbContext);

        Assert.Throws<ArgumentException>(() => service.SubmitLessonMistakes(userId: 1, lessonId: 1, new SubmitLessonMistakesRequest
        {
            Answers = new()
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 11, Answer = "world" },
                new SubmitExerciseAnswerRequest { ExerciseId = 11, Answer = "world" }
            }
        }));
    }

    

    [Fact]
    public void SubmitLessonMistakes_WhenNowPassed_ShouldUnlockNextLesson_AndUpdateProgress()
    {
        var dbContext = TestDbContextFactory.Create();

        // course/topic + 2 lessons
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
            new Lesson
            {
                Id = 1,
                TopicId = 1,
                Title = "Lesson 1",
                Theory = "Theory",
                Order = 1
            },
            new Lesson
            {
                Id = 2,
                TopicId = 1,
                Title = "Lesson 2",
                Theory = "Theory",
                Order = 2
            }
        );

        // 5 exercises -> 3 correct, 2 wrong (60%) initially
        dbContext.Exercises.AddRange(
            new Exercise { Id = 1, LessonId = 1, Order = 1, Type = ExerciseType.Input, Question = "Q1", CorrectAnswer = "a", Data = "{}" },
            new Exercise { Id = 2, LessonId = 1, Order = 2, Type = ExerciseType.Input, Question = "Q2", CorrectAnswer = "b", Data = "{}" },
            new Exercise { Id = 3, LessonId = 1, Order = 3, Type = ExerciseType.Input, Question = "Q3", CorrectAnswer = "c", Data = "{}" },
            new Exercise { Id = 4, LessonId = 1, Order = 4, Type = ExerciseType.Input, Question = "Q4", CorrectAnswer = "d", Data = "{}" },
            new Exercise { Id = 5, LessonId = 1, Order = 5, Type = ExerciseType.Input, Question = "Q5", CorrectAnswer = "e", Data = "{}" }
        );

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 1,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false
        });

        var details = new LessonResultDetailsJson
        {
            MistakeExerciseIds = new() { 4, 5 },
            Answers = new()
            {
                new LessonAnswerResultDto { ExerciseId = 1, UserAnswer = "a", CorrectAnswer = "a", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 2, UserAnswer = "b", CorrectAnswer = "b", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 3, UserAnswer = "c", CorrectAnswer = "c", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 4, UserAnswer = "WRONG", CorrectAnswer = "d", IsCorrect = false },
                new LessonAnswerResultDto { ExerciseId = 5, UserAnswer = "WRONG", CorrectAnswer = "e", IsCorrect = false }
            }
        };

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 1,
            Score = 3,
            TotalQuestions = 5,
            CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
            MistakesJson = JsonSerializer.Serialize(details)
        });

        dbContext.SaveChanges();

        var service = CreateService(dbContext);

        // fix only one mistake -> 4/5 = 80% pass, but still has 1 mistake left
        var response = service.SubmitLessonMistakes(userId: 1, lessonId: 1, new SubmitLessonMistakesRequest
        {
            Answers = new()
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 4, Answer = "d" }
            }
        });

        Assert.False(response.IsCompleted);
        Assert.Single(response.MistakeExerciseIds);
        Assert.Equal(5, response.TotalExercises);
        Assert.Equal(4, response.CorrectAnswers);

        var progress1 = dbContext.UserLessonProgresses.FirstOrDefault(x => x.UserId == 1 && x.LessonId == 1);
        Assert.NotNull(progress1);
        Assert.True(progress1!.IsCompleted);

        var progress2 = dbContext.UserLessonProgresses.FirstOrDefault(x => x.UserId == 1 && x.LessonId == 2);
        Assert.NotNull(progress2);
        Assert.True(progress2!.IsUnlocked);

        var userCourse = dbContext.UserCourses.FirstOrDefault(x => x.UserId == 1 && x.CourseId == 1 && x.IsActive);
        Assert.NotNull(userCourse);
        Assert.Equal(2, userCourse!.LastLessonId);

        var userProgress = dbContext.UserProgresses.FirstOrDefault(x => x.UserId == 1);
        Assert.NotNull(userProgress);
        Assert.Equal(1, userProgress!.CompletedLessons);
        Assert.Equal(4, userProgress.TotalScore);
    }

    [Fact]
    public void SubmitLessonMistakes_WhenNowPassed_ShouldAddVocabulary_AndCallAchievements()
    {
        var dbContext = TestDbContextFactory.Create();

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
            new Lesson
            {
                Id = 1,
                TopicId = 1,
                Title = "Lesson 1",
                Theory = "Theory",
                Order = 1
            },
            new Lesson
            {
                Id = 2,
                TopicId = 1,
                Title = "Lesson 2",
                Theory = "Theory",
                Order = 2
            }
        );

        dbContext.Exercises.AddRange(
            new Exercise { Id = 1, LessonId = 1, Order = 1, Type = ExerciseType.Input, Question = "Q1", CorrectAnswer = "a", Data = "{}" },
            new Exercise { Id = 2, LessonId = 1, Order = 2, Type = ExerciseType.Input, Question = "Q2", CorrectAnswer = "b", Data = "{}" },
            new Exercise { Id = 3, LessonId = 1, Order = 3, Type = ExerciseType.Input, Question = "Q3", CorrectAnswer = "c", Data = "{}" },
            new Exercise { Id = 4, LessonId = 1, Order = 4, Type = ExerciseType.Input, Question = "Q4", CorrectAnswer = "d", Data = "{}" },
            new Exercise { Id = 5, LessonId = 1, Order = 5, Type = ExerciseType.Input, Question = "Q5", CorrectAnswer = "e", Data = "{}" }
        );

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 1,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false
        });

        dbContext.VocabularyItems.AddRange(
            new VocabularyItem { Id = 100, Word = "theory", Translation = "t", Example = null },
            new VocabularyItem { Id = 101, Word = "mistake", Translation = "m", Example = null }
        );

        dbContext.LessonVocabularies.Add(new LessonVocabulary
        {
            LessonId = 1,
            VocabularyItemId = 100
        });

        dbContext.ExerciseVocabularies.Add(new ExerciseVocabulary
        {
            ExerciseId = 5,
            VocabularyItemId = 101
        });

        var details = new LessonResultDetailsJson
        {
            MistakeExerciseIds = new() { 4, 5 },
            Answers = new()
            {
                new LessonAnswerResultDto { ExerciseId = 1, UserAnswer = "a", CorrectAnswer = "a", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 2, UserAnswer = "b", CorrectAnswer = "b", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 3, UserAnswer = "c", CorrectAnswer = "c", IsCorrect = true },
                new LessonAnswerResultDto { ExerciseId = 4, UserAnswer = "WRONG", CorrectAnswer = "d", IsCorrect = false },
                new LessonAnswerResultDto { ExerciseId = 5, UserAnswer = "WRONG", CorrectAnswer = "e", IsCorrect = false }
            }
        };

        dbContext.LessonResults.Add(new LessonResult
        {
            Id = 1,
            UserId = 1,
            LessonId = 1,
            Score = 3,
            TotalQuestions = 5,
            CompletedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc),
            MistakesJson = JsonSerializer.Serialize(details)
        });

        dbContext.SaveChanges();

        var service = CreateService(dbContext, out var achievementService);

        service.SubmitLessonMistakes(userId: 1, lessonId: 1, new SubmitLessonMistakesRequest
        {
            Answers = new()
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 4, Answer = "d" }
            }
        });

        var now = new DateTime(2026, 02, 16, 12, 0, 0, DateTimeKind.Utc);

        var theoryUserWord = dbContext.UserVocabularies.FirstOrDefault(x => x.UserId == 1 && x.VocabularyItemId == 100);
        Assert.NotNull(theoryUserWord);
        Assert.Equal(now.AddDays(1), theoryUserWord!.NextReviewAt);

        var mistakeUserWord = dbContext.UserVocabularies.FirstOrDefault(x => x.UserId == 1 && x.VocabularyItemId == 101);
        Assert.NotNull(mistakeUserWord);
        Assert.Equal(now, mistakeUserWord!.NextReviewAt);

        Assert.Equal(1, achievementService.CallsCount);
        Assert.Equal(1, achievementService.LastUserId);
        Assert.Equal(4, achievementService.LastLessonScore);
        Assert.Equal(5, achievementService.LastTotalQuestions);
    }


    private static LessonMistakesService CreateService(Lumino.Api.Data.LuminoDbContext dbContext)
    {
        return CreateService(dbContext, out _);
    }

    private static LessonMistakesService CreateService(Lumino.Api.Data.LuminoDbContext dbContext, out FakeAchievementService achievementService)
    {
        var now = new DateTime(2026, 02, 16, 12, 0, 0, DateTimeKind.Utc);

        achievementService = new FakeAchievementService();

        var settings = Options.Create(new LearningSettings
        {
            PassingScorePercent = 80,
            SceneUnlockEveryLessons = 1
        });

        return new LessonMistakesService(dbContext, achievementService, new FixedDateTimeProvider(now), settings);
    }

    private class FakeAchievementService : IAchievementService
    {
        public int CallsCount { get; private set; }

        public int LastUserId { get; private set; }

        public int LastLessonScore { get; private set; }

        public int LastTotalQuestions { get; private set; }

        public void CheckAndGrantAchievements(int userId, int lessonScore, int totalQuestions)
        {
            CallsCount++;
            LastUserId = userId;
            LastLessonScore = lessonScore;
            LastTotalQuestions = totalQuestions;
        }

        public void CheckAndGrantSceneAchievements(int userId)
        {
        }
    }

    private static void SeedLessonBase(Lumino.Api.Data.LuminoDbContext dbContext, int lessonId, bool isUnlocked)
    {
        if (!dbContext.Courses.Any(x => x.Id == 1))
        {
            dbContext.Courses.Add(new Course
            {
                Id = 1,
                Title = "Course",
                Description = "Desc",
                IsPublished = true
            });
        }

        if (!dbContext.Topics.Any(x => x.Id == 1))
        {
            dbContext.Topics.Add(new Topic
            {
                Id = 1,
                CourseId = 1,
                Title = "Topic",
                Order = 1
            });
        }

        if (!dbContext.Lessons.Any(x => x.Id == lessonId))
        {
            dbContext.Lessons.Add(new Lesson
            {
                Id = lessonId,
                TopicId = 1,
                Title = "Lesson",
                Theory = "Theory",
                Order = 1
            });
        }

        var progress = dbContext.UserLessonProgresses
            .FirstOrDefault(x => x.UserId == 1 && x.LessonId == lessonId);

        if (progress == null)
        {
            dbContext.UserLessonProgresses.Add(new UserLessonProgress
            {
                UserId = 1,
                LessonId = lessonId,
                IsUnlocked = isUnlocked,
                IsCompleted = false
            });
        }
        else
        {
            progress.IsUnlocked = isUnlocked;
            progress.IsCompleted = false;
        }

        dbContext.SaveChanges();
    }
}
