using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/learning")]
    [Authorize]
    public class LearningController : ControllerBase
    {
        private readonly ICourseProgressService _courseProgressService;
        private readonly INextActivityService _nextActivityService;
        private readonly ICourseCompletionService _courseCompletionService;

        public LearningController(
            ICourseProgressService courseProgressService,
            INextActivityService nextActivityService,
            ICourseCompletionService courseCompletionService)
        {
            _courseProgressService = courseProgressService;
            _nextActivityService = nextActivityService;
            _courseCompletionService = courseCompletionService;
        }

        [HttpPost("courses/{courseId}/start")]
        public IActionResult StartCourse(int courseId)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _courseProgressService.StartCourse(userId, courseId);
            return Ok(result);
        }

        [HttpGet("courses/active")]
        public IActionResult GetMyActiveCourse()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _courseProgressService.GetMyActiveCourse(userId);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("courses/{courseId}/lessons/progress")]
        public IActionResult GetMyLessonProgressByCourse(int courseId)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _courseProgressService.GetMyLessonProgressByCourse(userId, courseId);
            return Ok(result);
        }

        // completion курсу (по уроках)
        [HttpGet("courses/{courseId}/completion/me")]
        public IActionResult GetMyCourseCompletion(int courseId)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _courseCompletionService.GetMyCourseCompletion(userId, courseId);
            return Ok(result);
        }

        // alias до /api/next/me
        [HttpGet("next")]
        public IActionResult GetMyNext()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            var next = _nextActivityService.GetNext(userId);

            if (next == null)
            {
                return NoContent();
            }

            return Ok(next);
        }
    }
}
