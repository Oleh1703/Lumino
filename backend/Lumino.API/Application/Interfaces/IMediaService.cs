using Lumino.Api.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Lumino.Api.Application.Interfaces
{
    public interface IMediaService
    {
        UploadMediaResponse Upload(IFormFile file, string baseUrl);
    }
}
