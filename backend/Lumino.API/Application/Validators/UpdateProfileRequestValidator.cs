using Lumino.Api.Application.DTOs;
using Lumino.Api.Utils;

namespace Lumino.Api.Application.Validators
{
    public class UpdateProfileRequestValidator : IUpdateProfileRequestValidator
    {
        public void Validate(UpdateProfileRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var value = request.Username.Trim();

                if (value.Length < 3 || value.Length > 32)
                {
                    throw new ArgumentException("Username length must be between 3 and 32 characters");
                }

                if (value.Contains(" "))
                {
                    throw new ArgumentException("Username must not contain spaces");
                }
            }

            SupportedAvatars.Validate(request.AvatarUrl, "AvatarUrl");

            if (!string.IsNullOrWhiteSpace(request.Theme))
            {
                var value = request.Theme.Trim().ToLowerInvariant();

                if (value != "light" && value != "dark")
                {
                    throw new ArgumentException("Theme is invalid");
                }
            }
        }
    }
}
