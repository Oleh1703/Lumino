using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lumino.Api.Utils;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/lessons")]
    [Authorize]
    public class LessonByIdController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonByIdController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet("{id}")]
        public IActionResult GetLesson(int id)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _lessonService.GetLessonById(userId, id);
            return Ok(result);
        }
    }
}
