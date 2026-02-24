using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lumino.Api.Utils;
using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserAccountService _userAccountService;
        private readonly IUserEconomyService _userEconomyService;

        public UserController(IUserService userService, IUserAccountService userAccountService, IUserEconomyService userEconomyService)
        {
            _userService = userService;
            _userAccountService = userAccountService;
            _userEconomyService = userEconomyService;
        }

        [HttpGet("me")]
        public IActionResult GetMe()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _userEconomyService.RefreshHearts(userId);
            var result = _userService.GetCurrentUser(userId);
            return Ok(result);
        }

        [HttpPut("profile")]
        public IActionResult UpdateProfile(UpdateProfileRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _userService.UpdateProfile(userId, request);
            return Ok(result);
        }

        [HttpPost("change-password")]
        public IActionResult ChangePassword(ChangePasswordRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _userAccountService.ChangePassword(userId, request);
            return NoContent();
        }

        [HttpPost("restore-hearts")]
        public IActionResult RestoreHearts(RestoreHeartsRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _userEconomyService.RestoreHearts(userId, request);
            return Ok(result);
        }
    }
}
