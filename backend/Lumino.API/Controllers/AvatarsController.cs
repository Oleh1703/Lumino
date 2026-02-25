using Lumino.Api.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/avatars")]
    public class AvatarsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AvatarsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(SupportedAvatars.GetAllowed(_configuration));
        }
    }
}
