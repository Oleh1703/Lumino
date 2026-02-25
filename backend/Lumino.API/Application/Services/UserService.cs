using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;

namespace Lumino.Api.Application.Services
{
    public class UserService : IUserService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IUpdateProfileRequestValidator _updateProfileRequestValidator;

        public UserService(LuminoDbContext dbContext, IUpdateProfileRequestValidator updateProfileRequestValidator)
        {
            _dbContext = dbContext;
            _updateProfileRequestValidator = updateProfileRequestValidator;
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
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                NativeLanguageCode = user.NativeLanguageCode,
                TargetLanguageCode = user.TargetLanguageCode,
                Hearts = user.Hearts,
                Crystals = user.Crystals,
                Theme = string.IsNullOrWhiteSpace(user.Theme) ? "light" : user.Theme
            };
        }

        public UserProfileResponse UpdateProfile(int userId, UpdateProfileRequest request)
        {
            _updateProfileRequestValidator.Validate(request);

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var username = request.Username.Trim();

                var exists = _dbContext.Users.Any(x => x.Username == username && x.Id != userId);

                if (exists)
                {
                    throw new ArgumentException("Username already exists");
                }

                user.Username = username;
            }

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            {
                user.AvatarUrl = request.AvatarUrl.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Theme))
            {
                user.Theme = request.Theme.Trim().ToLowerInvariant();
            }

            _dbContext.SaveChanges();

            return GetCurrentUser(userId);
        }
    }
}
