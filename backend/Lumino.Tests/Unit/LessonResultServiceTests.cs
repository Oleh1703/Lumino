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
    public void SubmitLesson_WhenUserAnswerHasExtraSpacesAndCase_ShouldStillBeCorrect()
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
            CorrectAnswer = "Good Morning",
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 2,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q2",
            Data = "",
            CorrectAnswer = "HELLO",
            Order = 2
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
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "  good   morning  " },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "  hello " }
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

    [Fact]
    public void SubmitLesson_WhenPassed_OrderZero_ShouldUnlockNextLesson_ByIdFallback()
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
            Id = 5,
            CourseId = 1,
            Title = "Basics",
            Order = 0
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 10,
            TopicId = 5,
            Title = "Lesson 10",
            Theory = "T",
            Order = 0
        });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 20,
            TopicId = 5,
            Title = "Lesson 20",
            Theory = "T",
            Order = 0
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 100,
            LessonId = 10,
            Type = ExerciseType.Input,
            Question = "Q1",
            Data = "",
            CorrectAnswer = "ok",
            Order = 1
        });

        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 10,
            LessonId = 10,
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
            LessonId = 10,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 100, Answer = "ok" }
            }
        });

        Assert.True(response.IsPassed);

        var p2 = dbContext.UserLessonProgresses.First(x => x.UserId == 10 && x.LessonId == 20);
        Assert.True(p2.IsUnlocked);

        var activeCourse = dbContext.UserCourses.First(x => x.UserId == 10 && x.CourseId == 1);
        Assert.Equal(20, activeCourse.LastLessonId);
    }

    [Fact]
    public void SubmitLesson_WhenPassed_ShouldAddLessonWordsToUserVocabulary_ByLessonVocabulary_EvenIfTheoryEmpty()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            Title = "Hello lesson",
            Theory = "",
            TopicId = 1,
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 1,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q1",
            Data = "{}",
            CorrectAnswer = "ok",
            Order = 1
        });

        dbContext.VocabularyItems.Add(new VocabularyItem
        {
            Id = 1,
            Word = "hello",
            Translation = "привіт",
            Example = null
        });

        dbContext.LessonVocabularies.Add(new LessonVocabulary
        {
            LessonId = 1,
            VocabularyItemId = 1
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

        var now = new DateTime(2026, 2, 13, 12, 0, 0, DateTimeKind.Utc);

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

        var userWords = dbContext.UserVocabularies
            .Where(x => x.UserId == 10)
            .ToList();

        Assert.Single(userWords);

        var item = dbContext.VocabularyItems.First(x => x.Id == userWords[0].VocabularyItemId);

        Assert.Equal("hello", item.Word);
        Assert.Equal("привіт", item.Translation);

        Assert.Equal(now.AddDays(1), userWords[0].NextReviewAt);
    }

    [Fact]
    public void SubmitLesson_WhenPassed_WithMistake_ShouldAddMistakeWordDueNow_ByExerciseVocabulary()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Lessons.Add(new Lesson
        {
            Id = 1,
            Title = "Lesson 1",
            Theory = "",
            TopicId = 1,
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 1,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q1",
            Data = "{}",
            CorrectAnswer = "right",
            Order = 1
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 2,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q2",
            Data = "{}",
            CorrectAnswer = "a",
            Order = 2
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 3,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q3",
            Data = "{}",
            CorrectAnswer = "b",
            Order = 3
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 4,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q4",
            Data = "{}",
            CorrectAnswer = "c",
            Order = 4
        });

        dbContext.Exercises.Add(new Exercise
        {
            Id = 5,
            LessonId = 1,
            Type = ExerciseType.Input,
            Question = "Q5",
            Data = "{}",
            CorrectAnswer = "d",
            Order = 5
        });

        dbContext.VocabularyItems.Add(new VocabularyItem
        {
            Id = 1,
            Word = "coffee",
            Translation = "кава",
            Example = null
        });

        dbContext.ExerciseVocabularies.Add(new ExerciseVocabulary
        {
            ExerciseId = 1,
            VocabularyItemId = 1
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

        var now = new DateTime(2026, 2, 14, 9, 0, 0, DateTimeKind.Utc);

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
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "wrong" },
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "a" },
                new SubmitExerciseAnswerRequest { ExerciseId = 3, Answer = "b" },
                new SubmitExerciseAnswerRequest { ExerciseId = 4, Answer = "c" },
                new SubmitExerciseAnswerRequest { ExerciseId = 5, Answer = "d" }
            }
        });

        Assert.True(response.IsPassed);
        Assert.Contains(1, response.MistakeExerciseIds);

        var userWord = dbContext.UserVocabularies
            .FirstOrDefault(x => x.UserId == 10 && x.VocabularyItemId == 1);

        Assert.NotNull(userWord);

        Assert.Equal(now, userWord!.NextReviewAt);
    }

    [Fact]
    public void SubmitLesson_WhenCompletesAllLessonsInCourse_ShouldMarkUserCourseCompleted()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Courses.Add(new Course
        {
            Id = 1,
            Title = "Course 1",
            Description = "D",
            IsPublished = true
        });

        dbContext.Topics.Add(new Topic
        {
            Id = 1,
            CourseId = 1,
            Title = "Topic 1",
            Order = 1
        });

        dbContext.Lessons.AddRange(
            new Lesson
            {
                Id = 1,
                Title = "Lesson 1",
                Theory = "Text",
                TopicId = 1,
                Order = 1
            },
            new Lesson
            {
                Id = 2,
                Title = "Lesson 2",
                Theory = "Text",
                TopicId = 1,
                Order = 2
            }
        );

        dbContext.Exercises.AddRange(
            new Exercise
            {
                Id = 1,
                LessonId = 1,
                Type = ExerciseType.Input,
                Question = "Q1",
                Data = "",
                CorrectAnswer = "a",
                Order = 1
            },
            new Exercise
            {
                Id = 2,
                LessonId = 2,
                Type = ExerciseType.Input,
                Question = "Q2",
                Data = "",
                CorrectAnswer = "b",
                Order = 1
            }
        );

        // урок 1 має бути unlocked для цього користувача (інакше SubmitLesson кине "Lesson is locked")
        dbContext.UserLessonProgresses.Add(new UserLessonProgress
        {
            UserId = 1,
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

        var r1 = service.SubmitLesson(1, new SubmitLessonRequest
        {
            LessonId = 1,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 1, Answer = "a" }
            }
        });

        Assert.True(r1.IsPassed);

        var r2 = service.SubmitLesson(1, new SubmitLessonRequest
        {
            LessonId = 2,
            Answers = new List<SubmitExerciseAnswerRequest>
            {
                new SubmitExerciseAnswerRequest { ExerciseId = 2, Answer = "b" }
            }
        });

        Assert.True(r2.IsPassed);

        var userCourse = dbContext.UserCourses.FirstOrDefault(x => x.UserId == 1 && x.CourseId == 1);

        Assert.NotNull(userCourse);
        Assert.True(userCourse!.IsCompleted);
        Assert.NotNull(userCourse.CompletedAt);
    }

}
