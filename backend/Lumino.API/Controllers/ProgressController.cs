using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lumino.Api.Utils;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/progress")]
    [Authorize]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressService _progressService;

        public ProgressController(IProgressService progressService)
        {
            _progressService = progressService;
        }

        [HttpGet("me")]
        public IActionResult GetMyProgress()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _progressService.GetMyProgress(userId);
            return Ok(result);
        }
    }
}
