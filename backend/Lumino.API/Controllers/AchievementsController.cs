using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lumino.Api.Utils;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/achievements")]
    [Authorize]
    public class AchievementsController : ControllerBase
    {
        private readonly IAchievementQueryService _achievementQueryService;

        public AchievementsController(IAchievementQueryService achievementQueryService)
        {
            _achievementQueryService = achievementQueryService;
        }

        [HttpGet("me")]
        public IActionResult GetMyAchievements()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _achievementQueryService.GetUserAchievements(userId);

            return Ok(result);
        }
    }
}
