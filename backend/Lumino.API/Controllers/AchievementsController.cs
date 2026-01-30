using Lumino.Api.Application.DTOs;
using Lumino.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lumino.Api.Controllers
{
    [ApiController]
    [Route("api/achievements")]
    [Authorize]
    public class AchievementsController : ControllerBase
    {
        private readonly LuminoDbContext _dbContext;

        public AchievementsController(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("me")]
        public IActionResult GetMyAchievements()
        {
            var userId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("sub")!
            );

            var result = _dbContext.UserAchievements
                .Where(x => x.UserId == userId)
                .Join(
                    _dbContext.Achievements,
                    ua => ua.AchievementId,
                    a => a.Id,
                    (ua, a) => new AchievementResponse
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Description = a.Description,
                        EarnedAt = ua.EarnedAt
                    }
                )
                .ToList();

            return Ok(result);
        }
    }
}
