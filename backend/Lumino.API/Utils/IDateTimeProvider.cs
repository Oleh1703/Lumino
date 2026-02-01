namespace Lumino.API.Utils
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
