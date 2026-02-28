using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lumino.Tests;

public class VocabularyReviewSkipTests
{
    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    [Fact]
    public void ReviewWord_Skip_DoesNotResetReviewCount_AndSetsShortDelay()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.VocabularyItems.Add(new VocabularyItem
        {
            Id = 1,
            Word = "test",
            Translation = "тест",
            Example = "test"
        });

        dbContext.UserVocabularies.Add(new UserVocabulary
        {
            Id = 10,
            UserId = 1,
            VocabularyItemId = 1,
            AddedAt = DateTime.UtcNow.AddDays(-5),
            LastReviewedAt = DateTime.UtcNow.AddDays(-1),
            NextReviewAt = DateTime.UtcNow.AddMinutes(-1),
            ReviewCount = 3
        });

        dbContext.SaveChanges();

        var now = new DateTime(2026, 02, 27, 12, 0, 0, DateTimeKind.Utc);
        var dt = new FixedDateTimeProvider(now);

        var settings = Options.Create(new LearningSettings
        {
            VocabularySkipDelayMinutes = 10,
            VocabularyWrongDelayHours = 12
        });

        var service = new VocabularyService(dbContext, dt, settings);

        service.ReviewWord(1, 10, new ReviewVocabularyRequest
        {
            Action = "skip",
            IsCorrect = false
        });

        var entity = dbContext.UserVocabularies.First(x => x.Id == 10);

        Assert.Equal(3, entity.ReviewCount);
        Assert.Equal(now.AddMinutes(10), entity.NextReviewAt);
    }
}
