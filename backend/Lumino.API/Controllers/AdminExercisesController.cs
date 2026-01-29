using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/admin/exercises")]
    [Authorize(Roles = "Admin")]
    public class AdminExercisesController : ControllerBase
    {
        private readonly IAdminExerciseService _adminExerciseService;

        public AdminExercisesController(IAdminExerciseService adminExerciseService)
        {
            _adminExerciseService = adminExerciseService;
        }

        [HttpGet("{lessonId}")]
        public IActionResult GetByLesson(int lessonId)
        {
            return Ok(_adminExerciseService.GetByLesson(lessonId));
        }

        [HttpPost]
        public IActionResult Create(CreateExerciseRequest request)
        {
            var result = _adminExerciseService.Create(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateExerciseRequest request)
        {
            _adminExerciseService.Update(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _adminExerciseService.Delete(id);
            return NoContent();
        }
    }
}
