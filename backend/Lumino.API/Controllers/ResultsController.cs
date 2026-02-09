using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lumino.API.Utils;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/results")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly ILessonResultQueryService _lessonResultQueryService;

        public ResultsController(ILessonResultQueryService lessonResultQueryService)
        {
            _lessonResultQueryService = lessonResultQueryService;
        }

        [HttpGet("me")]
        public IActionResult GetMyResults()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _lessonResultQueryService.GetMyResults(userId);
            return Ok(result);
        }
    }
}
