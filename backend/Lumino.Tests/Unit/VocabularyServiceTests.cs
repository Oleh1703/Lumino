﻿using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class VocabularyServiceTests
{
    [Fact]
    public void AddWord_ShouldCreateVocabularyItem_AndUserVocabulary()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "hello",
            Translation = "привіт",
            Example = "Hello, world!"
        });

        var items = dbContext.VocabularyItems.ToList();
        var userWords = dbContext.UserVocabularies.ToList();

        Assert.Single(items);
        Assert.Single(userWords);

        Assert.Equal("hello", items[0].Word);
        Assert.Equal("привіт", items[0].Translation);

        Assert.Equal(1, userWords[0].UserId);
        Assert.Equal(items[0].Id, userWords[0].VocabularyItemId);

        Assert.Equal(now, userWords[0].AddedAt);
        Assert.Equal(now, userWords[0].NextReviewAt);
        Assert.Null(userWords[0].LastReviewedAt);
        Assert.Equal(0, userWords[0].ReviewCount);
    }

    [Fact]
    public void AddWord_WhenAlreadyAdded_ShouldNotDuplicateUserVocabulary()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "hello",
            Translation = "привіт"
        });

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "hello",
            Translation = "привіт"
        });

        Assert.Single(dbContext.VocabularyItems);
        Assert.Single(dbContext.UserVocabularies);
    }

    [Fact]
    public void GetDueVocabulary_AfterAdd_ShouldReturnWord()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "cat",
            Translation = "кіт"
        });

        var due = service.GetDueVocabulary(userId: 1);

        Assert.Single(due);
        Assert.Equal("cat", due[0].Word);
    }

    [Fact]
    public void ReviewWord_Correct_ShouldIncreaseReviewCount_AndSetNextReviewAt()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "dog",
            Translation = "пес"
        });

        var entity = dbContext.UserVocabularies.First();

        var response = service.ReviewWord(userId: 1, userVocabularyId: entity.Id, new ReviewVocabularyRequest
        {
            IsCorrect = true
        });

        Assert.Equal(1, response.ReviewCount);
        Assert.NotNull(response.LastReviewedAt);

        Assert.Equal(now, response.LastReviewedAt!.Value);
        Assert.Equal(now.AddDays(1), response.NextReviewAt);

        var due = service.GetDueVocabulary(userId: 1);
        Assert.Empty(due);
    }

    [Fact]
    public void ReviewWord_Wrong_ShouldResetReviewCount_AndSetNextReviewAt12h()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "bird",
            Translation = "птах"
        });

        var entity = dbContext.UserVocabularies.First();

        service.ReviewWord(userId: 1, userVocabularyId: entity.Id, new ReviewVocabularyRequest
        {
            IsCorrect = true
        });

        var response = service.ReviewWord(userId: 1, userVocabularyId: entity.Id, new ReviewVocabularyRequest
        {
            IsCorrect = false
        });

        Assert.Equal(0, response.ReviewCount);
        Assert.NotNull(response.LastReviewedAt);

        Assert.Equal(now, response.LastReviewedAt!.Value);
        Assert.Equal(now.AddHours(12), response.NextReviewAt);
    }

    [Fact]
    public void ReviewWord_NotFound_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.ReviewWord(userId: 1, userVocabularyId: 999, new ReviewVocabularyRequest
            {
                IsCorrect = true
            });
        });
    }

    [Fact]
    public void DeleteWord_NotFound_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.DeleteWord(userId: 1, userVocabularyId: 999);
        });
    }
}
