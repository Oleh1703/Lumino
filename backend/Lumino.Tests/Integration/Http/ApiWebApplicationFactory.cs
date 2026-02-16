using Lumino.Api;
using Lumino.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lumino.Tests.Integration.Http;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "Lumino_TestDb_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Прибираємо INFO-логи (EF Core "Saved X entities..." і т.д.) у тестах
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();

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
