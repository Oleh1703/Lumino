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

        // повний контент сцени (кроки), якщо unlocked
        [HttpGet("{id}/content")]
        [Authorize]
        public IActionResult GetContent(int id)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            var result = _sceneService.GetSceneContent(userId, id);
            return Ok(result);
        }

        // помилки останньої спроби (кроки для повторення)
        [HttpGet("{id}/mistakes")]
        [Authorize]
        public IActionResult GetMistakes(int id)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            var result = _sceneService.GetSceneMistakes(userId, id);
            return Ok(result);
        }

        // submit сцени (перевірка choices, score, mistakes)
        [HttpPost("{id}/submit")]
        [Authorize]
        public IActionResult Submit(int id, [FromBody] SubmitSceneRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);

            var result = _sceneService.SubmitScene(userId, id, request);
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
