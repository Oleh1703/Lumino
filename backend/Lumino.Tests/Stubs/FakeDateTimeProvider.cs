using Lumino.API.Utils;

namespace Lumino.Tests;

public class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
