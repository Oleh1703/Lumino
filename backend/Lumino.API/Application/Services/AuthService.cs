using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Lumino.Api.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly LuminoDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IRegisterRequestValidator _registerRequestValidator;
        private readonly ILoginRequestValidator _loginRequestValidator;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(
            LuminoDbContext dbContext,
            IConfiguration configuration,
            IRegisterRequestValidator registerRequestValidator,
            ILoginRequestValidator loginRequestValidator,
            IPasswordHasher passwordHasher)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _registerRequestValidator = registerRequestValidator;
            _loginRequestValidator = loginRequestValidator;
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

            var user = new User
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = Role.User,
                CreatedAt = DateTime.UtcNow
            };

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

            var user = _dbContext.Users.FirstOrDefault(x => x.Email == request.Email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // ✅ Auto-upgrade legacy SHA256 to PBKDF2
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

            LimitActiveTokens(userId);

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
