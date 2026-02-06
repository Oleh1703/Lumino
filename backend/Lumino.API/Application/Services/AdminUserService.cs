using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;

namespace Lumino.Api.Application.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly LuminoDbContext _dbContext;

        public AdminUserService(LuminoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AdminUserResponse> GetAll()
        {
            return _dbContext.Users
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new AdminUserResponse
                {
                    Id = x.Id,
                    Email = x.Email,
                    Role = x.Role.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToList();
        }
    }
}
