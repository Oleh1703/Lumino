using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/admin/scenes")]
    [Authorize]
    public class AdminScenesController : ControllerBase
    {
        private readonly ISceneService _sceneService;

        public AdminScenesController(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        [HttpPost]
        public IActionResult Create(SceneResponse request)
        {
            _sceneService.CreateScene(request);
            return NoContent();
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, SceneResponse request)
        {
            _sceneService.UpdateScene(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _sceneService.DeleteScene(id);
            return NoContent();
        }
    }
}
