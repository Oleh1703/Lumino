using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Data;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Utils;
using Lumino.Tests.Stubs;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Lumino.Tests.Unit
{
    public class AuthOAuthTests
    {
        [Fact]
        public void OAuthGoogle_WhenUserDoesNotExist_ShouldCreateUser_AndReturnTokens()
        {
            var dbContext = TestDbContextFactory.Create();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "super_secret_test_key_1234567890" },
                    { "Jwt:Issuer", "test-issuer" },
                    { "Jwt:Audience", "test-audience" },
                    { "Jwt:ExpiresMinutes", "60" },
                    { "RefreshToken:ExpiresDays", "7" },
                    { "OAuth:Google:ClientId", "test-client-id" }
                })
                .Build();

            var registerValidator = new RegisterRequestValidator(configuration);
            var loginValidator = new LoginRequestValidator();
            var forgotValidator = new ForgotPasswordRequestValidator();
            var resetValidator = new ResetPasswordRequestValidator();

            var openIdValidator = new FakeOpenIdTokenValidator
            {
                GoogleUserInfo = new OpenIdUserInfo
                {
                    Subject = "sub-1",
                    Email = "newuser@example.com",
                    Name = "New User",
                    PictureUrl = null
                }
            };

            var service = new AuthService(
                dbContext,
                configuration,
                registerValidator,
                loginValidator,
                forgotValidator,
                resetValidator,
                new VerifyEmailRequestValidator(),
                new ResendVerificationRequestValidator(),
                new FakeEmailSender(),
                openIdValidator,
                new FakeHostEnvironment("Testing"),
                new PasswordHasher()
            );

            var result = service.OAuthGoogle(new OAuthLoginRequest
            {
                IdToken = "dummy"
            });

            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));

            var user = dbContext.Users.FirstOrDefault(x => x.Email == "newuser@example.com");
            Assert.NotNull(user);

            var external = dbContext.UserExternalLogins.FirstOrDefault(x => x.UserId == user!.Id && x.Provider == "google");
            Assert.NotNull(external);
        }

        [Fact]
        public void OAuthGoogle_WhenUserExistsByEmail_ShouldLinkExternalLogin()
        {
            var dbContext = TestDbContextFactory.Create();

            var existing = new User
            {
                Email = "exists@example.com",
                PasswordHash = new PasswordHasher().Hash("123456"),
                CreatedAt = DateTime.UtcNow,
                Role = Lumino.Api.Domain.Enums.Role.User
            };
            dbContext.Users.Add(existing);
            dbContext.SaveChanges();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "super_secret_test_key_1234567890" },
                    { "Jwt:Issuer", "test-issuer" },
                    { "Jwt:Audience", "test-audience" },
                    { "Jwt:ExpiresMinutes", "60" },
                    { "RefreshToken:ExpiresDays", "7" },
                    { "OAuth:Google:ClientId", "test-client-id" }
                })
                .Build();

            var service = new AuthService(
                dbContext,
                configuration,
                new RegisterRequestValidator(configuration),
                new LoginRequestValidator(),
                new ForgotPasswordRequestValidator(),
                new ResetPasswordRequestValidator(),
                new VerifyEmailRequestValidator(),
                new ResendVerificationRequestValidator(),
                new FakeEmailSender(),
                new FakeOpenIdTokenValidator
                {
                    GoogleUserInfo = new OpenIdUserInfo
                    {
                        Subject = "sub-2",
                        Email = "exists@example.com",
                        Name = "Exists",
                        PictureUrl = null
                    }
                },
                new FakeHostEnvironment("Testing"),
                new PasswordHasher()
            );

            service.OAuthGoogle(new OAuthLoginRequest { IdToken = "dummy" });

            var external = dbContext.UserExternalLogins.FirstOrDefault(x => x.Provider == "google" && x.ProviderUserId == "sub-2");
            Assert.NotNull(external);
            Assert.Equal(existing.Id, external!.UserId);
        }
    }
}
