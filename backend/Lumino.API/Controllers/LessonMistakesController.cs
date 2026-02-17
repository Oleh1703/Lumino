using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/lessons")]
    [Authorize]
    public class LessonMistakesController : ControllerBase
    {
        private readonly ILessonMistakesService _lessonMistakesService;

        public LessonMistakesController(ILessonMistakesService lessonMistakesService)
        {
            _lessonMistakesService = lessonMistakesService;
        }

        // помилки останньої спроби (вправи для повторення)
        [HttpGet("{id}/mistakes")]
        public IActionResult GetMistakes(int id)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            var result = _lessonMistakesService.GetLessonMistakes(userId, id);
            return Ok(result);
        }

        // submit тільки помилок (Duolingo repeat mistakes)
        [HttpPost("{id}/mistakes/submit")]
        public IActionResult SubmitMistakes(int id, [FromBody] SubmitLessonMistakesRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            var result = _lessonMistakesService.SubmitLessonMistakes(userId, id, request);
            return Ok(result);
        }
    }
}
