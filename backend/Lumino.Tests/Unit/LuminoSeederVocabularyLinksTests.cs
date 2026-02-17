using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using System.Reflection;
using Xunit;

namespace Lumino.Tests;

public class LuminoSeederVocabularyLinksTests
{
    [Fact]
    public void SeedLessonVocabularyLinks_WhenTheoryWithoutPairs_ShouldLinkExistingVocabularyItems()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.VocabularyItems.Add(new VocabularyItem { Id = 1, Word = "one", Translation = "один" });
        dbContext.VocabularyItems.Add(new VocabularyItem { Id = 2, Word = "two", Translation = "два" });
        dbContext.VocabularyItems.Add(new VocabularyItem { Id = 3, Word = "three", Translation = "три" });
        dbContext.VocabularyItems.Add(new VocabularyItem { Id = 4, Word = "four", Translation = "чотири" });
        dbContext.VocabularyItems.Add(new VocabularyItem { Id = 5, Word = "five", Translation = "п'ять" });

        dbContext.Lessons.Add(new Lesson
        {
            Id = 10,
            TopicId = 1,
            Title = "Numbers 1-5",
            Theory = "One, Two, Three, Four, Five",
            Order = 1
        });

        dbContext.SaveChanges();

        InvokePrivateSeedLessonVocabularyLinks(dbContext);

        var links = dbContext.LessonVocabularies
            .Where(x => x.LessonId == 10)
            .ToList();

        Assert.Equal(5, links.Count);

        Assert.Contains(links, x => x.VocabularyItemId == 1);
        Assert.Contains(links, x => x.VocabularyItemId == 2);
        Assert.Contains(links, x => x.VocabularyItemId == 3);
        Assert.Contains(links, x => x.VocabularyItemId == 4);
        Assert.Contains(links, x => x.VocabularyItemId == 5);

        // Не повинно створювати нові VocabularyItems для непарного Theory
        Assert.Equal(5, dbContext.VocabularyItems.Count());
    }

    private static void InvokePrivateSeedLessonVocabularyLinks(LuminoDbContext dbContext)
    {
        var method = typeof(LuminoSeeder).GetMethod(
            "SeedLessonVocabularyLinks",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        method!.Invoke(null, new object[] { dbContext });
    }
}
