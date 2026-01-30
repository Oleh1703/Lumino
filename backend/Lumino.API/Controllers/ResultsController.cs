using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            var userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("sub")!
            );

            var result = _lessonResultQueryService.GetMyResults(userId);
            return Ok(result);
        }
    }
}
