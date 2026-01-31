using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/scenes")]
    public class ScenesController : ControllerBase
    {
        private readonly ISceneService _sceneService;

        public ScenesController(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_sceneService.GetAllScenes());
        }

        [HttpPost("complete")]
        [Authorize]
        public IActionResult Complete(MarkSceneCompletedRequest request)
        {
            var userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            _sceneService.MarkCompleted(userId, request.SceneId);
            return NoContent();
        }

        [HttpGet("completed")]
        [Authorize]
        public IActionResult GetCompleted()
        {
            var userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            return Ok(_sceneService.GetCompletedScenes(userId));
        }
    }
}
