using Lumino.Api.Utils;
using Microsoft.Extensions.Options;

namespace Lumino.Tests;

public static class TestLearningSettingsFactory
{
    public static IOptions<LearningSettings> Create(LearningSettings? overrideSettings = null)
    {
        return Options.Create(overrideSettings ?? new LearningSettings());
    }
}
