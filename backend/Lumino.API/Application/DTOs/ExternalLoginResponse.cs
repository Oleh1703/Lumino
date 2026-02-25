using System;

namespace Lumino.Api.Application.DTOs
{
    public class ExternalLoginResponse
    {
        public string Provider { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; }
    }
}
