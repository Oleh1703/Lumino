﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class LessonResultServiceTests
{
    [Fact]
    public void SubmitLesson_ShouldCreateLessonResult_AndUpdateProgress()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            Title = "Lesson 1",
            Theory = "Text",
            TopicId = 1,
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 1,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q1",
            Data = "",
            CorrectAnswer = "hello",
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 2,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q2",
            Data = "",
            CorrectAnswer = "world",
            Order = 2
        });

        // урок має бути unlocked для цього користувача
        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 10,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false,
            BestScore = 0,
            LastAttemptAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var service = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            new FakeDateTimeProvider(),
            new FakeSubmitLessonValidator(),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var response = service.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "hello" },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "world" }
            }
        });

        Assert.Equal(2, response.TotalExercises);
        Assert.Equal(2, response.CorrectAnswers);
        Assert.True(response.IsPassed);
    }

    [Fact]
    public void SubmitLesson_WhenPassed_ShouldAddLessonWordsToUserVocabulary()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            Title = "Hello lesson",
            Theory = "hello = привіт\nthank you = дякую",
            TopicId = 1,
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 1,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Write Ukrainian for: hello",
            Data = "{}",
            CorrectAnswer = "привіт",
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 2,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Write Ukrainian for: thank you",
            Data = "{}",
            CorrectAnswer = "дякую",
            Order = 2
        });

        // урок має бути unlocked для цього користувача
        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 10,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false,
            BestScore = 0,
            LastAttemptAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc);

        var service = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            new FixedDateTimeProvider(now),
            new FakeSubmitLessonValidator(),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var response = service.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "привіт" },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "дякую" }
            }
        });

        Assert.True(response.IsPassed);

        var userWords = dbContext.UserVocabularies
            .Where(x => x.UserId == 10)
            .ToList();

        Assert.Equal(2, userWords.Count);

        var vocabIds = userWords.Select(x => x.VocabularyItemId).ToList();

        var items = dbContext.VocabularyItems
            .Where(x => vocabIds.Contains(x.Id))
            .ToList();

        Assert.Contains(items, x => x.Word == "hello" && x.Translation == "привіт");
        Assert.Contains(items, x => x.Word == "thank you" && x.Translation == "дякую");

        // без помилок — повторення завтра
        Assert.All(userWords, x => Assert.Equal(now.AddDays(1), x.NextReviewAt));
    }

    [Fact]
    public void SubmitLesson_WhenPassed_ShouldUnlockNextLesson_AndMoveActiveCourseLastLessonId()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "English A1",
            Description = "Desc",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Basics",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            TopicId = 1,
            Title = "Lesson 1",
            Theory = "T",
            Order = 1
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 2,
            TopicId = 1,
            Title = "Lesson 2",
            Theory = "T",
            Order = 2
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 1,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q1",
            Data = "",
            CorrectAnswer = "ok",
            Order = 1
        });

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 10,
            LessonId = 1,
            IsUnlocked = true,
            IsCompleted = false,
            BestScore = 0,
            LastAttemptAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);

        var service = new LessonResultService(
            dbContext,
            new FakeAchievementService(),
            new FixedDateTimeProvider(now),
            new FakeSubmitLessonValidator(),
            Options.Create(new LearningSettings { PassingScorePercent = 80 })
        );

        var response = service.SubmitLesson(10, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "ok" }
            }
        });

        Assert.True(response.IsPassed);

        var p1 = dbContext.UserLessonProgresses.First(x => x.UserId == 10 && x.LessonId == 1);
        Assert.True(p1.IsCompleted);

        var p2 = dbContext.UserLessonProgresses.First(x => x.UserId == 10 && x.LessonId == 2);
        Assert.True(p2.IsUnlocked);

        var activeCourse = dbContext.UserCourses.First(x => x.UserId == 10 && x.CourseId == 1);
        Assert.True(activeCourse.IsActive);
        Assert.Equal(2, activeCourse.LastLessonId);
    }
}
