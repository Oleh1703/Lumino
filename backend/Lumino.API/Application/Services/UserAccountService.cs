using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IChangePasswordRequestValidator _changePasswordRequestValidator;
        private readonly IPasswordHasher _passwordHasher;

        public UserAccountService(
            LuminoDbContext dbContext,
            IChangePasswordRequestValidator changePasswordRequestValidator,
            IPasswordHasher passwordHasher)
        {
            _dbContext = dbContext;
            _changePasswordRequestValidator = changePasswordRequestValidator;
            _passwordHasher = passwordHasher;
        }

        public void ChangePassword(int userId, ChangePasswordRequest request)
        {
            _changePasswordRequestValidator.Validate(request);

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            var ok = _passwordHasher.Verify(request.OldPassword, user.PasswordHash);

            if (!ok)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            _dbContext.SaveChanges();
        }
    }
}
