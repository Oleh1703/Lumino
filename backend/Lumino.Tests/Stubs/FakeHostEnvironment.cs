using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Lumino.Tests;

public class FakeHostEnvironment : IHostEnvironment
{
    public FakeHostEnvironment()
    {
    }

    public FakeHostEnvironment(string environmentName)
    {
        EnvironmentName = environmentName;
    }

    public string EnvironmentName { get; set; } = Environments.Development;

    public string ApplicationName { get; set; } = "Lumino.Tests";

    public string ContentRootPath { get; set; } = string.Empty;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
