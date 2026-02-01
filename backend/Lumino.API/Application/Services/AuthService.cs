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

        public AuthService(
            LuminoDbContext dbContext,
            IConfiguration configuration,
            IRegisterRequestValidator registerRequestValidator,
            ILoginRequestValidator loginRequestValidator)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _registerRequestValidator = registerRequestValidator;
            _loginRequestValidator = loginRequestValidator;
        }

        public AuthResponse Register(RegisterRequest request)
        {
            _registerRequestValidator.Validate(request);

            var existingUser = _dbContext.Users.FirstOrDefault(x => x.Email == request.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("User already exists");
            }

            var passwordHash = HashPassword(request.Password);

            var user = new User
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = Role.User,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = token
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

            var isPasswordValid = VerifyPassword(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Token = token
            };
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var hash = HashPassword(password);
            return hash == storedHash;
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
