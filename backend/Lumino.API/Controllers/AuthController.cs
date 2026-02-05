using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            var result = _authService.Register(request);
            return Ok(result);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var result = _authService.Login(request);
            return Ok(result);
        }

        [HttpPost("refresh")]
        public IActionResult Refresh(RefreshTokenRequest request)
        {
            var result = _authService.Refresh(request);
            return Ok(result);
        }

        [HttpPost("logout")]
        public IActionResult Logout(RefreshTokenRequest request)
        {
            _authService.Logout(request);
            return NoContent();
        }
    }
}
