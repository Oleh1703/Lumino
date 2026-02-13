using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/lessons/{lessonId}/exercises")]
    [Authorize]
    public class ExercisesController : ControllerBase
    {
        private readonly IExerciseService _exerciseService;

        public ExercisesController(IExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }

        [HttpGet]
        public IActionResult GetExercises(int lessonId)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _exerciseService.GetExercisesByLesson(userId, lessonId);
            return Ok(result);
        }
    }
}
