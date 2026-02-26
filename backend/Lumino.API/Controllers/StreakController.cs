using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/streak")]
    [Authorize]
    public class StreakController : ControllerBase
    {
        private readonly IStreakService _streakService;

        public StreakController(IStreakService streakService)
        {
            _streakService = streakService;
        }

        [HttpGet("me")]
        public IActionResult GetMyStreak()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            return Ok(_streakService.GetMyStreak(userId));
        }

        [HttpGet("calendar")]
        public IActionResult GetMyCalendar([FromQuery] int days = 30)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            return Ok(_streakService.GetMyCalendar(userId, days));
        }
    }
}
