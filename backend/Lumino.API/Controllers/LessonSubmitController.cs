using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/lessons/submit")]
    [Authorize]
    public class LessonSubmitController : ControllerBase
    {
        private readonly ILessonResultService _lessonResultService;

        public LessonSubmitController(ILessonResultService lessonResultService)
        {
            _lessonResultService = lessonResultService;
        }

        [HttpPost]
        public IActionResult Submit(SubmitLessonRequest request)
        {
            var userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("sub")!
            );

            var result = _lessonResultService.SubmitLesson(userId, request);
            return Ok(result);
        }
    }
}
