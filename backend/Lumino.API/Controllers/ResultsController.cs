using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lumino.Api.Utils;

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

        // деталі конкретної спроби (включно з відповідями і правильними відповідями)
        [HttpGet("me/{resultId}")]
        public IActionResult GetMyResultDetails(int resultId)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _lessonResultQueryService.GetMyResultDetails(userId, resultId);
            return Ok(result);
        }
    }
}
