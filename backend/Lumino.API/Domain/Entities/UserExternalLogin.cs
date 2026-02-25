using System.ComponentModel.DataAnnotations;

namespace Lumino.Api.Domain.Entities
{
    public class UserExternalLogin
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [MaxLength(20)]
        public string Provider { get; set; } = null!;

        [MaxLength(200)]
        public string ProviderUserId { get; set; } = null!;

        [MaxLength(256)]
        public string? Email { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public User? User { get; set; }
    }
}
