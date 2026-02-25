using Lumino.Api;
using Lumino.Api.Application.Interfaces;
using Lumino.Api.Data;
using Lumino.Tests.Stubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Lumino.Tests.Integration.Http;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "Lumino_TestDb_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Щоб тести не залежали від реального appsettings.json (Demo:LessonIds може змінитися),
            // фіксуємо demo-уроки прямо тут.
            // ВАЖЛИВО: AddInMemoryCollection приймає IEnumerable<KeyValuePair<string, string?>>,
            // тому тут використовуємо string? щоб не ловити попередження CS8620 (nullability mismatch).
            var demoConfig = new Dictionary<string, string?>
            {
                ["Demo:LessonIds:0"] = "1",
                ["Demo:LessonIds:1"] = "2",
                ["Demo:LessonIds:2"] = "3"
                ,["Demo:LanguageLessonIds:en:0"] = "1"
                ,["Demo:LanguageLessonIds:en:1"] = "2"
                ,["Demo:LanguageLessonIds:en:2"] = "3"
                , ["Demo:LanguageLessonIds:de:0"] = "4"
                , ["Demo:LanguageLessonIds:de:1"] = "5"
                , ["Demo:LanguageLessonIds:de:2"] = "6"

                // JWT/RefreshToken конфіг для інтеграційних тестів.
                // У WebApplicationFactory content root часто = Lumino.Tests, тому appsettings API може не підтягнутися.
                // Якщо не задати ці значення тут - GenerateJwtToken()/CreateRefreshToken можуть падати ArgumentNullException,
                // а ти отримаєш 400 BadRequest замість очікуваного 200 OK.
                , ["Jwt:Key"] = "TEST_JWT_KEY_32_CHARS_MINIMUM_123456"
                , ["Jwt:Issuer"] = "Lumino.Api"
                , ["Jwt:Audience"] = "Lumino.Client"
                , ["Jwt:ExpiresMinutes"] = "60"

                , ["RefreshToken:ExpiresDays"] = "7"
                , ["RefreshToken:MaxActiveTokens"] = "3"
                , ["RefreshToken:KeepRevokedDays"] = "30"

            };

            config.AddInMemoryCollection(demoConfig);
        });

        // Прибираємо INFO-логи (EF Core "Saved X entities..." і т.д.) у тестах
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();

            // Глобально відсікаємо все нижче Warning, щоб у виводі dotnet test не було INFO-логів контролерів
            logging.AddFilter((category, level) => level >= LogLevel.Warning);

            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<LuminoDbContext>));

            services.AddDbContext<LuminoDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                options => { }
            );

            // У тестах нам потрібен контрольований email sender, щоб перевіряти forgot-password без реального SMTP.
            services.RemoveAll(typeof(IEmailSender));
            services.AddSingleton<IEmailSender, FakeEmailSender>();
        });
    }
}
