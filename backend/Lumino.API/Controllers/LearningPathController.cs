using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/learning")]
    [Authorize]
    public class LearningPathController : ControllerBase
    {
        private readonly ILearningPathService _learningPathService;

        public LearningPathController(ILearningPathService learningPathService)
        {
            _learningPathService = learningPathService;
        }

        [HttpGet("courses/{courseId}/path/me")]
        public IActionResult GetMyCoursePath(int courseId)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            return Ok(_learningPathService.GetMyCoursePath(userId, courseId));
        }
    }
}
