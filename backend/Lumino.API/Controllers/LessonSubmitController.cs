using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lumino.Api.Utils;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/lesson-submit")]
    [Authorize]
    public class LessonSubmitController : ControllerBase
    {
        private readonly ILessonResultService _lessonResultService;

        public LessonSubmitController(ILessonResultService lessonResultService)
        {
            _lessonResultService = lessonResultService;
        }

        
        [HttpPost("/api/lessons/{id}/submit")]
        public IActionResult SubmitLessonAlias(int id, [FromBody] SubmitLessonRequest request)
        {
            if (request == null)
            {
                return BadRequest();
            }

            // route has priority (design contract: /api/lessons/{id}/submit)
            request.LessonId = id;

            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _lessonResultService.SubmitLesson(userId, request);
            return Ok(result);
        }

        [HttpPost]
        public IActionResult SubmitLesson([FromBody] SubmitLessonRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _lessonResultService.SubmitLesson(userId, request);
            return Ok(result);
        }
    }
}
