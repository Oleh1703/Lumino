using Lumino.Api.Application.DTOs;

namespace Lumino.Api.Application.Validators
{
    public class ChangePasswordRequestValidator : IChangePasswordRequestValidator
    {
        public void Validate(ChangePasswordRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.OldPassword))
            {
                throw new ArgumentException("OldPassword is required");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                throw new ArgumentException("NewPassword is required");
            }

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                throw new ArgumentException("ConfirmPassword is required");
            }

            if (request.NewPassword.Length < 6)
            {
                throw new ArgumentException("NewPassword must be at least 6 characters");
            }

            if (request.NewPassword.Length > 64)
            {
                throw new ArgumentException("NewPassword must be at most 64 characters");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ArgumentException("Passwords do not match");
            }

            if (request.OldPassword == request.NewPassword)
            {
                throw new ArgumentException("NewPassword must be different from OldPassword");
            }
        }
    }
}
