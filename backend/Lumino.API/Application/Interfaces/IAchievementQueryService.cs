using Lumino.Api.Application.DTOs;
using System.Collections.Generic;

namespace Lumino.Api.Application.Interfaces
{
    public interface IAchievementQueryService
    {
        List<AchievementResponse> GetUserAchievements(int userId);
    }
}
