using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/admin/scenes")]
    [Authorize(Roles = "Admin")]
    public class AdminScenesController : ControllerBase
    {
        private readonly IAdminSceneService _adminSceneService;

        public AdminScenesController(IAdminSceneService adminSceneService)
        {
            _adminSceneService = adminSceneService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_adminSceneService.GetAll());
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            return Ok(_adminSceneService.GetById(id));
        }

        [HttpPost]
        public IActionResult Create(CreateSceneRequest request)
        {
            var result = _adminSceneService.Create(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateSceneRequest request)
        {
            _adminSceneService.Update(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _adminSceneService.Delete(id);
            return NoContent();
        }

        [HttpGet("{sceneId}/steps")]
        public IActionResult GetSteps(int sceneId)
        {
            return Ok(_adminSceneService.GetSteps(sceneId));
        }

        [HttpPost("{sceneId}/steps")]
        public IActionResult AddStep(int sceneId, CreateSceneStepRequest request)
        {
            var result = _adminSceneService.AddStep(sceneId, request);
            return Ok(result);
        }

        [HttpPut("{sceneId}/steps/{stepId}")]
        public IActionResult UpdateStep(int sceneId, int stepId, UpdateSceneStepRequest request)
        {
            _adminSceneService.UpdateStep(sceneId, stepId, request);
            return NoContent();
        }

        [HttpDelete("{sceneId}/steps/{stepId}")]
        public IActionResult DeleteStep(int sceneId, int stepId)
        {
            _adminSceneService.DeleteStep(sceneId, stepId);
            return NoContent();
        }
    }
}
