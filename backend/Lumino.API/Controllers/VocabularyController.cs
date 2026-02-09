using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            return Ok(_vocabularyService.GetMyVocabulary(userId));
        }

        [HttpGet("due")]
        public IActionResult GetDue()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            return Ok(_vocabularyService.GetDueVocabulary(userId));
        }

        [HttpGet("review/next")]
        public IActionResult GetNextReview()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
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
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _vocabularyService.AddWord(userId, request);
            return NoContent();
        }

        [HttpPost("{id}/review")]
        public IActionResult Review(int id, ReviewVocabularyRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            var result = _vocabularyService.ReviewWord(userId, id, request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _vocabularyService.DeleteWord(userId, id);
            return NoContent();
        }
    }
}
