using Lumino.Api.Utils;
using Xunit;

namespace Lumino.Tests.Unit;

public class HeartsEconomyCalculatorTests
{
    [Fact]
    public void GetHeartsMax_ShouldFallbackToDefault_WhenSettingsIsNullOrInvalid()
    {
        Assert.Equal(5, HeartsEconomyCalculator.GetHeartsMax(null!));
        Assert.Equal(5, HeartsEconomyCalculator.GetHeartsMax(new LearningSettings { HeartsMax = 0 }));
        Assert.Equal(7, HeartsEconomyCalculator.GetHeartsMax(new LearningSettings { HeartsMax = 7 }));
    }

    [Fact]
    public void GetHeartRegenMinutes_ShouldFallbackToDefault_WhenSettingsIsNullOrInvalid()
    {
        Assert.Equal(30, HeartsEconomyCalculator.GetHeartRegenMinutes(null!));
        Assert.Equal(30, HeartsEconomyCalculator.GetHeartRegenMinutes(new LearningSettings { HeartRegenMinutes = 0 }));
        Assert.Equal(25, HeartsEconomyCalculator.GetHeartRegenMinutes(new LearningSettings { HeartRegenMinutes = 25 }));
    }

    [Fact]
    public void GetCrystalCostPerHeart_ShouldFallbackToDefault_WhenSettingsIsNullOrInvalid()
    {
        Assert.Equal(10, HeartsEconomyCalculator.GetCrystalCostPerHeart(null!));
        Assert.Equal(10, HeartsEconomyCalculator.GetCrystalCostPerHeart(new LearningSettings { CrystalCostPerHeart = 0 }));
        Assert.Equal(15, HeartsEconomyCalculator.GetCrystalCostPerHeart(new LearningSettings { CrystalCostPerHeart = 15 }));
    }

    [Fact]
    public void NextHeart_ShouldBeNull_WhenHeartsAreFull()
    {
        var settings = new LearningSettings { HeartsMax = 5, HeartRegenMinutes = 30 };

        var next = HeartsEconomyCalculator.GetNextHeartAtUtc(5, DateTime.UtcNow.AddMinutes(-100), settings);
        Assert.Null(next);

        var seconds = HeartsEconomyCalculator.GetNextHeartInSeconds(5, DateTime.UtcNow.AddMinutes(-100), settings);
        Assert.Equal(0, seconds);
    }

    [Fact]
    public void NextHeart_ShouldNeverBeInPast()
    {
        var settings = new LearningSettings { HeartsMax = 5, HeartRegenMinutes = 30 };

        // hearts updated long ago -> next would be in past, but calculator must clamp to nowUtc
        var next = HeartsEconomyCalculator.GetNextHeartAtUtc(1, DateTime.UtcNow.AddHours(-10), settings);
        Assert.NotNull(next);
        Assert.True(next!.Value >= DateTime.UtcNow.AddSeconds(-1));
    }
}
