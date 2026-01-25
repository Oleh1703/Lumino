using Lumino.Api.Domain.Enums;

namespace Lumino.Api.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public Role Role { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}

