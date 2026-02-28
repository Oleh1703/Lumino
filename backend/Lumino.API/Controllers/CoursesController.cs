using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpGet]
        public IActionResult GetCourses([FromQuery] string? languageCode)
        {
            var result = _courseService.GetPublishedCourses(languageCode);
            return Ok(result);
        }


        [HttpGet("me")]
        [Authorize]
        public IActionResult GetMyCourses([FromQuery] string? languageCode)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _courseService.GetMyCourses(userId, languageCode);
            return Ok(result);
        }

    }
}
