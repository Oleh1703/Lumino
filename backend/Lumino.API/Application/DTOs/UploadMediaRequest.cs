using Microsoft.AspNetCore.Http;

namespace Lumino.Api.Application.DTOs
{
    public class UploadMediaRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}
