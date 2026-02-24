using Lumino.Api.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/avatars")]
    public class AvatarsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(SupportedAvatars.All);
        }
    }
}
