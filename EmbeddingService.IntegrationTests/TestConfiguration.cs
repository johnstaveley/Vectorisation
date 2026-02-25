using Microsoft.Extensions.Configuration;

namespace EmbeddingService.IntegrationTests;

public sealed class TestConfiguration
{
    private static readonly Lazy<TestConfiguration> _instance = new(() => new TestConfiguration());

    public static TestConfiguration Instance => _instance.Value;

    public string WebServerUrl { get; }
    public int SampleSize { get; }

    private TestConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        WebServerUrl = configuration["TestSettings:WebServerUrl"] 
            ?? throw new InvalidOperationException("WebServerUrl not configured in appsettings.json under TestSettings section");

        SampleSize = int.TryParse(configuration["BulkLoad:SampleSize"], out var size) ? size : 100;
    }
}

