using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/onboarding")]
    public class OnboardingController : ControllerBase
    {
        private readonly IOnboardingService _onboardingService;

        public OnboardingController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        [HttpGet("languages")]
        [AllowAnonymous]
        public IActionResult GetSupportedLanguages()
        {
            var result = _onboardingService.GetSupportedLanguages();
            return Ok(result);
        }

                [HttpGet("languages/me")]
        [Authorize]
        public IActionResult GetMyLanguages()
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            return Ok(_onboardingService.GetMyLanguages(userId));
        }

        [HttpPut("languages/me")]
        [Authorize]
        public IActionResult UpdateMyLanguages([FromBody] UpdateUserLanguagesRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _onboardingService.UpdateMyLanguages(userId, request);
            return NoContent();
        }

        [HttpPut("target-language/me")]
        [Authorize]
        public IActionResult UpdateMyTargetLanguage([FromBody] UpdateTargetLanguageRequest request)
        {
            var userId = ClaimsUtils.GetUserIdOrThrow(User);
            _onboardingService.UpdateMyTargetLanguage(userId, request);
            return NoContent();
        }

        [HttpGet("languages/{languageCode}/availability")]
        [AllowAnonymous]
        public IActionResult GetLanguageAvailability(string languageCode)
        {
            var result = _onboardingService.GetLanguageAvailability(languageCode);
            return Ok(result);
        }
    }
}
