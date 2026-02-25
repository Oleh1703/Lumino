using Lumino.Api.Application.DTOs;
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

        Assert.Single(dbContext.VocabularyItemTranslations);
        Assert.Equal(items[0].Id, dbContext.VocabularyItemTranslations.First().VocabularyItemId);
        Assert.Equal("привіт", dbContext.VocabularyItemTranslations.First().Translation);
        Assert.Equal(0, dbContext.VocabularyItemTranslations.First().Order);

        Assert.Equal(1, userWords[0].UserId);
        Assert.Equal(items[0].Id, userWords[0].VocabularyItemId);

        Assert.Equal(now, userWords[0].AddedAt);
        Assert.Equal(now, userWords[0].NextReviewAt);
        Assert.Null(userWords[0].LastReviewedAt);
        Assert.Equal(0, userWords[0].ReviewCount);
    }


    [Fact]
    public void AddWord_WithMultipleTranslations_ShouldSaveAllTranslations_AndKeepPrimaryInTranslation()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "run",
            Translation = "бігти",
            Translations = new List<string> { "запускати", "бігти" }
        });

        Assert.Single(dbContext.VocabularyItems);

        var item = dbContext.VocabularyItems.First();
        Assert.Equal("run", item.Word);
        Assert.Equal("бігти", item.Translation);

        var translations = dbContext.VocabularyItemTranslations
            .Where(x => x.VocabularyItemId == item.Id)
            .OrderBy(x => x.Order)
            .Select(x => x.Translation)
            .ToList();

        Assert.Equal(2, translations.Count);
        Assert.Equal("бігти", translations[0]);
        Assert.Equal("запускати", translations[1]);

        var due = service.GetDueVocabulary(userId: 1);
        Assert.Single(due);

        Assert.Equal("бігти", due[0].Translation);
        Assert.Equal(2, due[0].Translations.Count);
    }


    [Fact]
    public void AddWord_SameWordWithDifferentPrimaryTranslation_ShouldNotCreateDuplicateItem_AndShouldReorderPrimary()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "run",
            Translation = "бігти",
            Translations = new List<string> { "запускати" }
        });

        // Add the same word again, but another translation as "primary".
        service.AddWord(userId: 2, new AddVocabularyRequest
        {
            Word = "run",
            Translation = "запускати"
        });

        Assert.Single(dbContext.VocabularyItems);

        var item = dbContext.VocabularyItems.First();
        Assert.Equal("run", item.Word);
        Assert.Equal("запускати", item.Translation);

        var translations = dbContext.VocabularyItemTranslations
            .Where(x => x.VocabularyItemId == item.Id)
            .OrderBy(x => x.Order)
            .Select(x => x.Translation)
            .ToList();

        Assert.Equal(2, translations.Count);
        Assert.Equal("запускати", translations[0]);
        Assert.Equal("бігти", translations[1]);

        Assert.Equal(2, dbContext.UserVocabularies.Count());
    }

    [Fact]
    public void AddWord_WhenTranslationsTableHasDuplicates_ShouldCleanupDuplicates_AndKeepUniqueSequentialOrders()
    {
        var dbContext = TestDbContextFactory.Create();

        // existing vocabulary item
        dbContext.VocabularyItems.Add(new Lumino.Api.Domain.Entities.VocabularyItem
        {
            Id = 1,
            Word = "run",
            Translation = "бігти"
        });

        dbContext.SaveChanges();

        // broken state: duplicated translations and conflicting orders
        dbContext.VocabularyItemTranslations.Add(new Lumino.Api.Domain.Entities.VocabularyItemTranslation
        {
            VocabularyItemId = 1,
            Translation = "бігти",
            Order = 0
        });

        dbContext.VocabularyItemTranslations.Add(new Lumino.Api.Domain.Entities.VocabularyItemTranslation
        {
            VocabularyItemId = 1,
            Translation = "бігти",
            Order = 1
        });

        dbContext.VocabularyItemTranslations.Add(new Lumino.Api.Domain.Entities.VocabularyItemTranslation
        {
            VocabularyItemId = 1,
            Translation = "запускати",
            Order = 1
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        // Add the same word with another primary translation - should reorder and cleanup duplicates
        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "run",
            Translation = "запускати"
        });

        var item = dbContext.VocabularyItems.First(x => x.Id == 1);
        Assert.Equal("запускати", item.Translation);

        var translations = dbContext.VocabularyItemTranslations
            .Where(x => x.VocabularyItemId == 1)
            .OrderBy(x => x.Order)
            .ToList();

        Assert.Equal(2, translations.Count);
        Assert.Equal(0, translations[0].Order);
        Assert.Equal("запускати", translations[0].Translation);
        Assert.Equal(1, translations[1].Order);
        Assert.Equal("бігти", translations[1].Translation);
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
    public void ReviewWord_WithSameIdempotencyKey_ShouldBeIdempotent()
    {
        var dbContext = TestDbContextFactory.Create();

        var now = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc);
        var service = new VocabularyService(dbContext, new FixedDateTimeProvider(now), Options.Create(new LearningSettings()));

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "fish",
            Translation = "риба"
        });

        var entity = dbContext.UserVocabularies.First();

        var first = service.ReviewWord(userId: 1, userVocabularyId: entity.Id, new ReviewVocabularyRequest
        {
            IsCorrect = true,
            IdempotencyKey = "review-1"
        });

        var second = service.ReviewWord(userId: 1, userVocabularyId: entity.Id, new ReviewVocabularyRequest
        {
            IsCorrect = true,
            IdempotencyKey = "review-1"
        });

        Assert.Equal(1, first.ReviewCount);
        Assert.Equal(1, second.ReviewCount);

        Assert.Equal(now, first.LastReviewedAt!.Value);
        Assert.Equal(now, second.LastReviewedAt!.Value);

        Assert.Equal(now.AddDays(1), first.NextReviewAt);
        Assert.Equal(now.AddDays(1), second.NextReviewAt);
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
