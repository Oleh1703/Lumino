namespace Lumino.Api.Application.DTOs
{
    public class UpdateProfileRequest
    {
        public string? Username { get; set; }

        public string? AvatarUrl { get; set; }

        public string? Theme { get; set; }
    }
}
