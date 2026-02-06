namespace Lumino.Api.Application.DTOs
{
    public class AdminUserResponse
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;

        public string Role { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
