using Microsoft.Extensions.Configuration;

namespace Lumino.Tests;

public static class TestConfigurationFactory
{
    public static IConfiguration Create()
    {
        var data = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "TEST_KEY_1234567890_TEST_KEY_1234567890",
            ["Jwt:Issuer"] = "Lumino.Test",
            ["Jwt:Audience"] = "Lumino.Test",
            ["Jwt:ExpiresMinutes"] = "60"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }
}
