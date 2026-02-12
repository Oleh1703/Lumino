using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Xunit;

namespace Lumino.Tests;

public class VocabularyServiceTests
{
    [Fact]
    public void AddWord_ShouldCreateVocabularyItem_AndUserVocabulary()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new VocabularyService(dbContext);

        var before = DateTime.UtcNow;

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

        // NextReviewAt = now (має бути дуже близько)
        Assert.True(userWords[0].NextReviewAt >= before.AddSeconds(-1));
        Assert.True(userWords[0].NextReviewAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void AddWord_WhenAlreadyAdded_ShouldNotDuplicateUserVocabulary()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new VocabularyService(dbContext);

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
        var service = new VocabularyService(dbContext);

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
        var service = new VocabularyService(dbContext);

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

        var last = response.LastReviewedAt!.Value;

        // reviewCount=1 => +1 day
        Assert.True(response.NextReviewAt >= last.AddHours(23));
        Assert.True(response.NextReviewAt <= last.AddHours(25));

        // Після correct review, слово зазвичай вже НЕ due
        var due = service.GetDueVocabulary(userId: 1);
        Assert.Empty(due);
    }

    [Fact]
    public void ReviewWord_Wrong_ShouldResetReviewCount_AndSetNextReviewAt12h()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new VocabularyService(dbContext);

        service.AddWord(userId: 1, new AddVocabularyRequest
        {
            Word = "bird",
            Translation = "птах"
        });

        var entity = dbContext.UserVocabularies.First();

        // Спочатку зробимо correct, щоб reviewCount став 1
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

        var last = response.LastReviewedAt!.Value;

        Assert.True(response.NextReviewAt >= last.AddHours(11));
        Assert.True(response.NextReviewAt <= last.AddHours(13));
    }

    [Fact]
    public void ReviewWord_NotFound_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        var service = new VocabularyService(dbContext);

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
        var service = new VocabularyService(dbContext);

        Assert.Throws<KeyNotFoundException>(() =>
        {
            service.DeleteWord(userId: 1, userVocabularyId: 999);
        });
    }
}
