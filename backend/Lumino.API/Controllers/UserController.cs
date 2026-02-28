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
        private readonly IUserExternalLoginService _userExternalLoginService;
        private readonly IStreakService _streakService;

        public UserController(IUserService userService, IUserAccountService userAccountService, IUserEconomyService userEconomyService, IUserExternalLoginService userExternalLoginService, IStreakService streakService)
        {
            _userService = userService;
            _userAccountService = userAccountService;
            _userEconomyService = userEconomyService;
            _userExternalLoginService = userExternalLoginService;
            _streakService = streakService;
        }

        [HttpGet("me")]
        public IActionResult GetMe()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _userEconomyService.RefreshHearts(userId);
            var result = _userService.GetCurrentUser(userId);

            var streak = _streakService.GetMyStreak(userId);
            result.CurrentStreakDays = streak.Current;
            result.BestStreakDays = streak.Best;

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

        [HttpPost("delete-account")]
        public IActionResult DeleteAccount(DeleteAccountRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _userAccountService.DeleteAccount(userId, request);
            return NoContent();
        }

        [HttpGet("external-logins")]
        public IActionResult GetExternalLogins()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _userExternalLoginService.GetExternalLogins(userId);
            return Ok(result);
        }

        [HttpPost("external-logins/unlink")]
        public IActionResult UnlinkExternalLogin(UnlinkExternalLoginRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _userExternalLoginService.UnlinkExternalLogin(userId, request);
            return NoContent();
        }

        [HttpPost("external-logins/link/google")]
        public IActionResult LinkGoogleExternalLogin(LinkExternalLoginRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _userExternalLoginService.LinkGoogleExternalLogin(userId, request);
            return NoContent();
        }

        [HttpPost("external-logins/link/apple")]
        public IActionResult LinkAppleExternalLogin(LinkExternalLoginRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _userExternalLoginService.LinkAppleExternalLogin(userId, request);
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
