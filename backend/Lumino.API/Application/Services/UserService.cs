using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;

namespace Lumino.Api.Application.Services
{
    public class UserService : IUserService
    {
        private readonly LuminoDbContext _dbContext;

        public UserService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public UserProfileResponse GetCurrentUser(int userId)
        {
            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            return new UserProfileResponse
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            };
        }
    }
}
