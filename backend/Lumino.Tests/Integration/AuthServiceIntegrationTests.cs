using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Utils;
using Lumino.Tests;
using Xunit;
using Lumino.Tests.Stubs;

namespace Lumino.Tests.Integration;

public class AuthServiceIntegrationTests
{
    [Fact]
    public void Register_ShouldRequireEmailVerification_Then_Verify_Then_Login_ShouldReturnToken()
    {
        var dbContext = TestDbContextFactory.Create();
        var configuration = TestConfigurationFactory.Create();

        var emailSender = new FakeEmailSender();

        var service = new AuthService(
            dbContext,
            configuration,
            new RegisterRequestValidator(configuration),
            new LoginRequestValidator(),
	        new ForgotPasswordRequestValidator(),
	        new ResetPasswordRequestValidator(),
	        new VerifyEmailRequestValidator(),
	        new ResendVerificationRequestValidator(),
	        emailSender,
	        new FakeOpenIdTokenValidator(),
	        new FakeHostEnvironment(),
	        new PasswordHasher()
        );

        var registerResponse = service.Register(new RegisterRequest
        {
            Email = "integration@mail.com",
            Password = "123456"
        });

        Assert.True(registerResponse.RequiresEmailVerification);
        Assert.True(string.IsNullOrWhiteSpace(registerResponse.Token));

        var token = ExtractTokenFromEmailBody(emailSender.LastHtmlBody);

        var verifyResponse = service.VerifyEmail(new VerifyEmailRequest
        {
            Token = token
        }, ip: null, userAgent: null);

        Assert.False(verifyResponse.RequiresEmailVerification);
        Assert.False(string.IsNullOrWhiteSpace(verifyResponse.Token));

        var loginResponse = service.Login(new LoginRequest
        {
            Email = "integration@mail.com",
            Password = "123456"
        });

        Assert.False(string.IsNullOrWhiteSpace(loginResponse.Token));
    }

    private static string ExtractTokenFromEmailBody(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new InvalidOperationException("Email body is empty");
        }

        // We expect either ...verify-email?token=... OR <b>{token}</b>
        var marker = "token=";
        var idx = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var start = idx + marker.Length;
            var end = html.IndexOf('"', start);
            if (end < 0)
            {
                end = html.IndexOf('<', start);
            }

            if (end > start)
            {
                return Uri.UnescapeDataString(html.Substring(start, end - start));
            }
        }

        var bOpen = html.IndexOf("<b>", StringComparison.OrdinalIgnoreCase);
        var bClose = html.IndexOf("</b>", StringComparison.OrdinalIgnoreCase);
        if (bOpen >= 0 && bClose > bOpen)
        {
            var raw = html.Substring(bOpen + 3, bClose - (bOpen + 3));
            if (!string.IsNullOrWhiteSpace(raw))
            {
                return raw.Trim();
            }
        }

        throw new InvalidOperationException("Cannot extract token from email body");
    }
}
