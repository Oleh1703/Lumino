using Lumino.Api.Utils;

namespace Lumino.Tests;

public class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
