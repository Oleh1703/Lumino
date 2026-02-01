using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/media")]
    [Authorize(Roles = "Admin")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public IActionResult Upload([FromForm] UploadMediaRequest request)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var result = _mediaService.Upload(request.File, baseUrl);

            return Ok(result);
        }
    }
}
