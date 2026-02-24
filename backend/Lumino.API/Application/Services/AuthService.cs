using Azure.Core;
using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Lumino.Api.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IRegisterRequestValidator _registerRequestValidator;
        private readonly ILoginRequestValidator _loginRequestValidator;
        private readonly IForgotPasswordRequestValidator _forgotPasswordRequestValidator;
        private readonly IResetPasswordRequestValidator _resetPasswordRequestValidator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IHostEnvironment _hostEnvironment;

        public AuthService(
            LuminoDbContext dbContext,
            IConfiguration configuration,
            IRegisterRequestValidator registerRequestValidator,
            ILoginRequestValidator loginRequestValidator,
            IForgotPasswordRequestValidator forgotPasswordRequestValidator,
            IResetPasswordRequestValidator resetPasswordRequestValidator,
            IHostEnvironment hostEnvironment,
            IPasswordHasher passwordHasher)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _registerRequestValidator = registerRequestValidator;
            _loginRequestValidator = loginRequestValidator;
            _forgotPasswordRequestValidator = forgotPasswordRequestValidator;
            _resetPasswordRequestValidator = resetPasswordRequestValidator;
            _hostEnvironment = hostEnvironment;
            _passwordHasher = passwordHasher;
        }

        public AuthResponse Register(RegisterRequest request)
        {
            _registerRequestValidator.Validate(request);

            var existingUser = _dbContext.Users.FirstOrDefault(x => x.Email == request.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("User already exists");
            }

            var passwordHash = _passwordHasher.Hash(request.Password);

            var native = string.IsNullOrWhiteSpace(request.NativeLanguageCode)
                ? null
                : request.NativeLanguageCode.Trim().ToLowerInvariant();

            var target = string.IsNullOrWhiteSpace(request.TargetLanguageCode)
                ? null
                : request.TargetLanguageCode.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(target) && string.IsNullOrWhiteSpace(native))
            {
                native = SupportedLanguages.DefaultNativeLanguageCode;
            }

            var user = new User
            {
                Username = NormalizeUsernameOrNull(request.Username),
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = Role.User,
                CreatedAt = DateTime.UtcNow,
                NativeLanguageCode = native,
                TargetLanguageCode = target,
                AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? SupportedAvatars.DefaultAvatarUrl : request.AvatarUrl!.Trim(),
                Hearts = 5,
                Crystals = 0,
                Theme = "light"
            };

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                user.Username = GenerateUniqueUsernameFromEmail(request.Email);
            }
            else
            {
                EnsureUsernameIsUniqueOrThrow(user.Username);
            }

            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();

            var accessToken = GenerateJwtToken(user);

            var refreshToken = CreateRefreshToken(user.Id);

            return new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }

        public AuthResponse Login(LoginRequest request)
        {
            _loginRequestValidator.Validate(request);

            var login = !string.IsNullOrWhiteSpace(request.Login)
                ? request.Login.Trim()
                : request.Email.Trim();

            User? user;

            if (login.Contains('@'))
            {
                user = _dbContext.Users.FirstOrDefault(x => x.Email == login);
            }
            else
            {
                user = _dbContext.Users.FirstOrDefault(x => x.Username == login);
            }
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Auto-upgrade legacy SHA256 to PBKDF2
            if (_passwordHasher.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = _passwordHasher.Hash(request.Password);
                _dbContext.SaveChanges();
            }

            var accessToken = GenerateJwtToken(user);

            var refreshToken = CreateRefreshToken(user.Id);

            return new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }

        public ForgotPasswordResponse ForgotPassword(ForgotPasswordRequest request, string? ip, string? userAgent)
        {
            _forgotPasswordRequestValidator.Validate(request);

            var email = request.Email.Trim();

            var user = _dbContext.Users.FirstOrDefault(x => x.Email == email);

            // Always respond OK to avoid account enumeration
            if (user == null)
            {
                return new ForgotPasswordResponse
                {
                    IsSent = true
                };
            }

            var rawToken = GeneratePasswordResetToken();
            var tokenHash = HashToken(rawToken);

            var nowUtc = DateTime.UtcNow;
            var expiresAtUtc = nowUtc.AddMinutes(30);

            var entity = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAt = nowUtc,
                ExpiresAt = expiresAtUtc,
                Ip = string.IsNullOrWhiteSpace(ip) ? null : ip.Trim(),
                UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim()
            };

            _dbContext.PasswordResetTokens.Add(entity);
            _dbContext.SaveChanges();

            // For diploma/testing we can return the token in Development/Testing.
            var canExposeToken = _hostEnvironment.IsDevelopment() || _hostEnvironment.IsEnvironment("Testing");

            return new ForgotPasswordResponse
            {
                IsSent = true,
                ResetToken = canExposeToken ? rawToken : null,
                ExpiresAtUtc = canExposeToken ? expiresAtUtc : null
            };
        }

        public void ResetPassword(ResetPasswordRequest request)
        {
            _resetPasswordRequestValidator.Validate(request);

            var token = request.Token.Trim();
            var tokenHash = HashToken(token);

            var nowUtc = DateTime.UtcNow;

            var reset = _dbContext.PasswordResetTokens
                .FirstOrDefault(x => x.TokenHash == tokenHash);

            if (reset == null)
            {
                throw new UnauthorizedAccessException("Invalid token");
            }

            if (reset.UsedAt != null)
            {
                throw new UnauthorizedAccessException("Token already used");
            }

            if (reset.ExpiresAt <= nowUtc)
            {
                throw new UnauthorizedAccessException("Token expired");
            }

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == reset.UserId);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid token");
            }

            user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
            reset.UsedAt = nowUtc;

            _dbContext.SaveChanges();
        }

        private string? NormalizeUsernameOrNull(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            return username.Trim();
        }

        private void EnsureUsernameIsUniqueOrThrow(string username)
        {
            var existing = _dbContext.Users.FirstOrDefault(x => x.Username == username);

            if (existing != null)
            {
                throw new ArgumentException("Username already exists");
            }
        }

        private string GenerateUniqueUsernameFromEmail(string email)
        {
            var local = email.Split('@')[0];

            var baseName = new string(local
                .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
                .ToArray());

            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "user";
            }

            if (baseName.Length > 20)
            {
                baseName = baseName.Substring(0, 20);
            }

            var candidate = baseName;
            int suffix = 1;

            while (_dbContext.Users.Any(x => x.Username == candidate))
            {
                suffix++;
                candidate = $"{baseName}{suffix}";

                if (candidate.Length > 32)
                {
                    candidate = candidate.Substring(0, 32);
                }
            }

            return candidate;
        }

        private static string GeneratePasswordResetToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            return Base64UrlEncode(bytes);
        }

        public AuthResponse Refresh(RefreshTokenRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new ArgumentException("RefreshToken is required");
            }

            var refreshTokenHash = HashToken(request.RefreshToken);

            var tokenEntity = _dbContext.RefreshTokens.FirstOrDefault(x => x.TokenHash == refreshTokenHash);
            if (tokenEntity == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            if (tokenEntity.RevokedAt != null)
            {
                throw new UnauthorizedAccessException("Refresh token revoked");
            }

            if (tokenEntity.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Refresh token expired");
            }

            var user = _dbContext.Users.FirstOrDefault(x => x.Id == tokenEntity.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            var newRefreshToken = GenerateRefreshToken();
            var newRefreshTokenHash = HashToken(newRefreshToken);

            tokenEntity.RevokedAt = DateTime.UtcNow;
            tokenEntity.ReplacedByTokenHash = newRefreshTokenHash;

            var refreshSection = _configuration.GetSection("RefreshToken");
            var expiresDaysText = refreshSection["ExpiresDays"];

            if (!int.TryParse(expiresDaysText, out var expiresDays))
            {
                expiresDays = 7;
            }

            if (expiresDays < 1)
            {
                expiresDays = 1;
            }

            var newEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expiresDays)
            };

            _dbContext.RefreshTokens.Add(newEntity);

            // Спочатку зберігаємо, щоб LimitActiveTokens працював по реальним даним
            _dbContext.SaveChanges();

            LimitActiveTokens(user.Id);

            _dbContext.SaveChanges();

            var newAccessToken = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public void Logout(RefreshTokenRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new ArgumentException("RefreshToken is required");
            }

            var refreshTokenHash = HashToken(request.RefreshToken);

            var tokenEntity = _dbContext.RefreshTokens.FirstOrDefault(x => x.TokenHash == refreshTokenHash);
            if (tokenEntity == null)
            {
                return;
            }

            if (tokenEntity.RevokedAt != null)
            {
                return;
            }

            tokenEntity.RevokedAt = DateTime.UtcNow;
            _dbContext.SaveChanges();
        }

        private void LimitActiveTokens(int userId)
        {
            var refreshSection = _configuration.GetSection("RefreshToken");
            var maxActiveTokensText = refreshSection["MaxActiveTokens"];

            if (!int.TryParse(maxActiveTokensText, out var maxActiveTokens))
            {
                maxActiveTokens = 3;
            }

            if (maxActiveTokens <= 0)
            {
                return;
            }

            var now = DateTime.UtcNow;

            var activeTokens = _dbContext.RefreshTokens
                .Where(x =>
                    x.UserId == userId
                    && x.RevokedAt == null
                    && x.ExpiresAt > now
                )
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            if (activeTokens.Count <= maxActiveTokens)
            {
                return;
            }

            var tokensToRevoke = activeTokens
                .Skip(maxActiveTokens)
                .ToList();

            foreach (var token in tokensToRevoke)
            {
                token.RevokedAt = now;
            }
        }

        private static string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            var base64 = Convert.ToBase64String(bytes);

            return base64
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private string CreateRefreshToken(int userId)
        {
            var refreshToken = GenerateRefreshToken();
            var refreshTokenHash = HashToken(refreshToken);

            var refreshSection = _configuration.GetSection("RefreshToken");
            var expiresDaysText = refreshSection["ExpiresDays"];

            if (!int.TryParse(expiresDaysText, out var expiresDays))
            {
                expiresDays = 7;
            }

            if (expiresDays < 1)
            {
                expiresDays = 1;
            }

            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = refreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expiresDays)
            };

            _dbContext.RefreshTokens.Add(entity);

            // Спочатку зберігаємо новий refresh token, щоб LimitActiveTokens бачив його у базі
            _dbContext.SaveChanges();

            LimitActiveTokens(userId);

            // Зберігаємо можливі revocations
            _dbContext.SaveChanges();

            return refreshToken;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(jwtSettings["ExpiresMinutes"]!)
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
