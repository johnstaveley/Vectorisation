namespace EmbeddingService.IntegrationTests;

public class TestConfigurationTests
{
    [Fact]
    public void TestConfiguration_LoadsWebServerUrl_Successfully()
    {
        // Arrange & Act
        var config = TestConfiguration.Instance;

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.WebServerUrl);
        Assert.NotEmpty(config.WebServerUrl);
        Assert.StartsWith("http", config.WebServerUrl);
    }

    [Fact]
    public void TestConfiguration_IsSingleton_ReturnsSameInstance()
    {
        // Arrange & Act
        var instance1 = TestConfiguration.Instance;
        var instance2 = TestConfiguration.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }
}
