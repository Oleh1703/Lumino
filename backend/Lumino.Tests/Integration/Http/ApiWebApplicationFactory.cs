using Lumino.Api;
using Lumino.Api.Data;
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
        });
    }
}
