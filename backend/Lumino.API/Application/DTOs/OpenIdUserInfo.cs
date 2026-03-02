namespace Lumino.Api.Application.DTOs
{
    public class OpenIdUserInfo
    {
        public string Subject { get; set; } = null!;

        // Some providers may omit email on subsequent logins. For Google we still require it.
        public string? Email { get; set; }

        public string? Name { get; set; }

        public string? PictureUrl { get; set; }
    }
}
