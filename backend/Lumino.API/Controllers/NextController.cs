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

        public NextController(INextActivityService nextActivityService)
        {
            _nextActivityService = nextActivityService;
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
    }
}
