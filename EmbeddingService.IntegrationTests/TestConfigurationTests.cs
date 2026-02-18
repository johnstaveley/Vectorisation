using FluentAssertions;

namespace EmbeddingService.IntegrationTests;

public class TestConfigurationTests
{
    [Fact]
    public void TestConfiguration_LoadsWebServerUrl_Successfully()
    {
        // Arrange & Act
        var config = TestConfiguration.Instance;

        // Assert
        config.Should().NotBeNull();
        config.WebServerUrl.Should().NotBeNull();
        config.WebServerUrl.Should().NotBeEmpty();
        config.WebServerUrl.Should().StartWith("http");
    }

    [Fact]
    public void TestConfiguration_IsSingleton_ReturnsSameInstance()
    {
        // Arrange & Act
        var instance1 = TestConfiguration.Instance;
        var instance2 = TestConfiguration.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }
}
