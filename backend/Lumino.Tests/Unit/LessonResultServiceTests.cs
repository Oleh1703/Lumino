using Lumino.Api.Application.DTOs;
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
}
