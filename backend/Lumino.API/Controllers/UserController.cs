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

        public UserController(IUserService userService, IUserAccountService userAccountService)
        {
            _userService = userService;
            _userAccountService = userAccountService;
        }

        [HttpGet("me")]
        public IActionResult GetMe()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
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
    }
}
