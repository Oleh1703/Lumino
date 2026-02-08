namespace Lumino.API.Utils
{
    public interface IPasswordHasher
    {
        string Hash(string password);

        bool Verify(string password, string storedHash);

        bool NeedsRehash(string storedHash);
    }
}
