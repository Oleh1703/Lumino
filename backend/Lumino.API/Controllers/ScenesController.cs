using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        // деталі сцени (locked/completed)
        [HttpGet("{id}")]
        [Authorize]
        public IActionResult GetById(int id)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            var result = _sceneService.GetSceneDetails(userId, id);
            return Ok(result);
        }

        [HttpPost("complete")]
        [Authorize]
        public IActionResult Complete(MarkSceneCompletedRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            _sceneService.MarkCompleted(userId, request.SceneId);
            return NoContent();
        }

        [HttpGet("completed")]
        [Authorize]
        public IActionResult GetCompleted()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            return Ok(_sceneService.GetCompletedScenes(userId));
        }
    }
}
