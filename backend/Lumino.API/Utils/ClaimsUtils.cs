using System.Security.Claims;

namespace Lumino.Api.Utils
{
    public static class ClaimsUtils
    {
        public static int GetUserIdOrThrow(ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(ClaimTypes.Name)
                ?? user.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(id) || !int.TryParse(id, out int userId) || userId <= 0)
            {
                throw new UnauthorizedAccessException("Invalid token");
            }

            return userId;
        }
    }
}
