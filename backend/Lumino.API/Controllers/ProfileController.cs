using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("weekly-progress")]
        public IActionResult GetWeeklyProgress()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            return Ok(_profileService.GetWeeklyProgress(userId));
        }
    }
}
