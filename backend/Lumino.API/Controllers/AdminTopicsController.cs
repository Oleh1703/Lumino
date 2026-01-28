using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/admin/topics")]
    [Authorize(Roles = "Admin")]
    public class AdminTopicsController : ControllerBase
    {
        private readonly IAdminTopicService _adminTopicService;

        public AdminTopicsController(IAdminTopicService adminTopicService)
        {
            _adminTopicService = adminTopicService;
        }

        [HttpGet("{courseId}")]
        public IActionResult GetByCourse(int courseId)
        {
            return Ok(_adminTopicService.GetByCourse(courseId));
        }

        [HttpPost]
        public IActionResult Create(CreateTopicRequest request)
        {
            var result = _adminTopicService.Create(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateTopicRequest request)
        {
            _adminTopicService.Update(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _adminTopicService.Delete(id);
            return NoContent();
        }
    }
}
