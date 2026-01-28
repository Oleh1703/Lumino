using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/admin/courses")]
    [Authorize(Roles = "Admin")]
    public class AdminCoursesController : ControllerBase
    {
        private readonly IAdminCourseService _adminCourseService;

        public AdminCoursesController(IAdminCourseService adminCourseService)
        {
            _adminCourseService = adminCourseService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_adminCourseService.GetAll());
        }

        [HttpPost]
        public IActionResult Create(CreateCourseRequest request)
        {
            var result = _adminCourseService.Create(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateCourseRequest request)
        {
            _adminCourseService.Update(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _adminCourseService.Delete(id);
            return NoContent();
        }
    }
}
