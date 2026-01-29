using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/topics/{topicId}/lessons")]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonsController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet]
        public IActionResult GetLessons(int topicId)
        {
            var result = _lessonService.GetLessonsByTopic(topicId);
            return Ok(result);
        }
    }
}
