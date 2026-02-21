using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/demo")]
    [AllowAnonymous]
    public class DemoController : ControllerBase
    {
        private readonly IDemoLessonService _demoLessonService;
        private readonly ILogger<DemoController> _logger;

        public DemoController(IDemoLessonService demoLessonService, ILogger<DemoController> logger)
        {
            _demoLessonService = demoLessonService;
            _logger = logger;
        }

        [HttpGet("lessons")]
        public IActionResult GetDemoLessons()
        {
            var result = _demoLessonService.GetDemoLessons();
            return Ok(result);
        }

        [HttpGet("next")]
        public IActionResult GetDemoNext([FromQuery] int step = 0)
        {
            var result = _demoLessonService.GetDemoNextLesson(step);

            if (result.Step == 0)
            {
                _logger.LogInformation("demo_started step={step} total={total}", result.Step, result.Total);
            }
            else
            {
                _logger.LogInformation("demo_next_requested step={step} total={total}", result.Step, result.Total);
            }

            return Ok(result);
        }

        [HttpGet("next-pack")]
        public IActionResult GetDemoNextPack([FromQuery] int step = 0)
        {
            var result = _demoLessonService.GetDemoNextLessonPack(step);

            if (result.Step == 0)
            {
                _logger.LogInformation("demo_started step={step} total={total}", result.Step, result.Total);
            }
            else
            {
                _logger.LogInformation("demo_next_pack_requested step={step} total={total}", result.Step, result.Total);
            }

            return Ok(result);
        }

        [HttpGet("lessons/{lessonId}")]
        public IActionResult GetDemoLessonById(int lessonId)
        {
            var result = _demoLessonService.GetDemoLessonById(lessonId);
            return Ok(result);
        }

        [HttpGet("lessons/{lessonId}/exercises")]
        public IActionResult GetDemoExercisesByLesson(int lessonId)
        {
            var result = _demoLessonService.GetDemoExercisesByLesson(lessonId);
            return Ok(result);
        }

        [HttpPost("lesson-submit")]
        public IActionResult SubmitDemoLesson([FromBody] SubmitLessonRequest request)
        {
            var result = _demoLessonService.SubmitDemoLesson(request);

            _logger.LogInformation(
                "demo_lesson_submitted lessonId={lessonId} isPassed={isPassed} correct={correct} total={total}",
                request.LessonId,
                result.IsPassed,
                result.CorrectAnswers,
                result.TotalExercises
            );

            if (result.IsPassed)
            {
                _logger.LogInformation("demo_lesson_passed lessonId={lessonId}", request.LessonId);
            }

            return Ok(result);
        }
    }
}
