using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/admin/lessons")]
    [Authorize(Roles = "Admin")]
    public class AdminLessonsController : ControllerBase
    {
        private readonly IAdminLessonService _adminLessonService;

        public AdminLessonsController(IAdminLessonService adminLessonService)
        {
            _adminLessonService = adminLessonService;
        }

        [HttpGet("{topicId}")]
        public IActionResult GetByTopic(int topicId)
        {
            return Ok(_adminLessonService.GetByTopic(topicId));
        }

        [HttpPost]
        public IActionResult Create(CreateLessonRequest request)
        {
            var result = _adminLessonService.Create(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateLessonRequest request)
        {
            _adminLessonService.Update(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _adminLessonService.Delete(id);
            return NoContent();
        }
    }
}
