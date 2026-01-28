using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/courses/{courseId}/topics")]
    public class TopicsController : ControllerBase
    {
        private readonly ITopicService _topicService;

        public TopicsController(ITopicService topicService)
        {
            _topicService = topicService;
        }

        [HttpGet]
        public IActionResult GetTopics(int courseId)
        {
            var result = _topicService.GetTopicsByCourse(courseId);
            return Ok(result);
        }
    }
}
