using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/next")]
    [Authorize]
    public class NextController : ControllerBase
    {
        private readonly INextActivityService _nextActivityService;
        private readonly IProgressService _progressService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public NextController(
            INextActivityService nextActivityService,
            IProgressService progressService,
            IDateTimeProvider dateTimeProvider)
        {
            _nextActivityService = nextActivityService;
            _progressService = progressService;
            _dateTimeProvider = dateTimeProvider;
        }

        [HttpGet("me")]
        public IActionResult GetMyNext()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            var next = _nextActivityService.GetNext(userId);

            if (next == null)
            {
                return NoContent();
            }

            return Ok(next);
        }

        [HttpGet("me/preview")]
        public IActionResult GetMyNextPreview()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            var next = _nextActivityService.GetNext(userId);
            var progress = _progressService.GetMyProgress(userId);

            var response = new NextPreviewResponse
            {
                Next = next,
                Progress = progress,
                GeneratedAt = _dateTimeProvider.UtcNow
            };

            return Ok(response);
        }
    }
}
