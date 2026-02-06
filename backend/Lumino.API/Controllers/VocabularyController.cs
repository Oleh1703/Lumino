using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/vocabulary")]
    [Authorize]
    public class VocabularyController : ControllerBase
    {
        private readonly IVocabularyService _vocabularyService;

        public VocabularyController(IVocabularyService vocabularyService)
        {
            _vocabularyService = vocabularyService;
        }

        [HttpGet("me")]
        public IActionResult GetMyVocabulary()
        {
            var userId = GetUserId();
            return Ok(_vocabularyService.GetMyVocabulary(userId));
        }

        [HttpGet("due")]
        public IActionResult GetDue()
        {
            var userId = GetUserId();
            return Ok(_vocabularyService.GetDueVocabulary(userId));
        }

        [HttpGet("review/next")]
        public IActionResult GetNextReview()
        {
            var userId = GetUserId();
            var item = _vocabularyService.GetNextReview(userId);

            if (item == null)
            {
                return NoContent();
            }

            return Ok(item);
        }

        [HttpPost]
        public IActionResult Add(AddVocabularyRequest request)
        {
            var userId = GetUserId();
            _vocabularyService.AddWord(userId, request);
            return NoContent();
        }

        [HttpPost("{id}/review")]
        public IActionResult Review(int id, ReviewVocabularyRequest request)
        {
            var userId = GetUserId();
            var result = _vocabularyService.ReviewWord(userId, id, request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var userId = GetUserId();
            _vocabularyService.DeleteWord(userId, id);
            return NoContent();
        }

        private int GetUserId()
        {
            var value =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("sub");

            return int.Parse(value!);
        }
    }
}
