using Lumino.Api.Utils;
using Xunit;

namespace Lumino.Tests;

public class TheoryVocabularyExtractorTests
{
    [Fact]
    public void ExtractNonPairWords_WhenCommaSeparatedNumbers_ShouldReturnNormalizedDistinctWords()
    {
        var theory = "One, Two, Three, Four, Five";

        var words = TheoryVocabularyExtractor.ExtractNonPairWords(theory);

        Assert.Equal(5, words.Count);
        Assert.Equal("one", words[0]);
        Assert.Equal("two", words[1]);
        Assert.Equal("three", words[2]);
        Assert.Equal("four", words[3]);
        Assert.Equal("five", words[4]);
    }

    [Fact]
    public void ExtractNonPairWords_WhenHasPairs_ShouldReturnEmpty()
    {
        var theory = "Hello = Привіт\nGoodbye = До побачення";

        var words = TheoryVocabularyExtractor.ExtractNonPairWords(theory);

        Assert.Empty(words);
    }
}
