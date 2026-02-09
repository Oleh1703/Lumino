using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lumino.Api.Utils;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public IActionResult GetMe()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _userService.GetCurrentUser(userId);
            return Ok(result);
        }
    }
}
