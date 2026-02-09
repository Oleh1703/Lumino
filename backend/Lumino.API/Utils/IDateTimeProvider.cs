namespace Lumino.Api.Utils
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
