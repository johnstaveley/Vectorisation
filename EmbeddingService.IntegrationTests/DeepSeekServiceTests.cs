using EmbeddingService.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace EmbeddingService.IntegrationTests;

[Collection("Integration Tests")]
public class DeepSeekServiceTests
{
    private readonly HttpClient _client;

    public DeepSeekServiceTests()
    {
        _client = new DefaultHttpClientFactory().CreateClient();
    }

    [Fact]
    public async Task DeepSeekGenerate_WithValidPrompt_ReturnsResponse()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "What is the capital of France?"
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result.Should().ContainKey("Response");
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"DeepSeek Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithCustomOptions_ReturnsResponse()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "Write a haiku about programming.",
            Options = new DeepSeekOptions
            {
                Temperature = 0.9,
                TopP = 0.95,
                MaxTokens = 500
            }
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Haiku Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithEmptyPrompt_ReturnsBadRequest()
    {
        var request = new DeepSeekRequest
        {
            Prompt = ""
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeepSeekGenerate_WithNullPrompt_ReturnsBadRequest()
    {
        var request = new DeepSeekRequest
        {
            Prompt = null!
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeepSeekChat_WithValidMessage_ReturnsResponse()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "Hello! How are you today?"
        };

        var response = await _client.PostAsJsonAsync("/deepseek/chat", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result.Should().ContainKey("Response");
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Chat Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekChat_WithEmptyMessage_ReturnsBadRequest()
    {
        var request = new DeepSeekRequest
        {
            Prompt = ""
        };

        var response = await _client.PostAsJsonAsync("/deepseek/chat", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDeepSeekModels_ReturnsModelList()
    {
        var response = await _client.GetAsync("/deepseek/models", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var models = await response.Content.ReadFromJsonAsync<List<OllamaModelInfo>>(cancellationToken: TestContext.Current.CancellationToken);
        models.Should().NotBeNull();
        models.Should().NotBeEmpty("at least the deepseek model should be available");
        
        Console.WriteLine($"Available models: {string.Join(", ", models!.Select(m => m.Name))}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithLongPrompt_ReturnsResponse()
    {
        var longPrompt = "Explain the concept of artificial intelligence in detail, including its history, " +
                        "current applications, and future potential. Cover machine learning, deep learning, " +
                        "neural networks, and their practical uses in modern technology.";

        var request = new DeepSeekRequest
        {
            Prompt = longPrompt,
            Options = new DeepSeekOptions
            {
                MaxTokens = 1000
            }
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        result["Response"].Length.Should().BeGreaterThan(100, "detailed response should be substantial");
        
        Console.WriteLine($"Long Response Length: {result["Response"].Length} characters");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithCodeRequest_ReturnsCode()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "Write a simple C# method that calculates the factorial of a number.",
            Options = new DeepSeekOptions
            {
                Temperature = 0.3
            }
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        result["Response"].Should().Contain("factorial", "response should be about factorial");
        
        Console.WriteLine($"Code Response:\n{result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekChat_MultipleMessages_MaintainsContext()
    {
        var firstMessage = new DeepSeekRequest
        {
            Prompt = "My name is Alice. Remember this."
        };

        var firstResponse = await _client.PostAsJsonAsync("/deepseek/chat", firstMessage, TestContext.Current.CancellationToken);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        firstResult.Should().NotBeNull();
        
        Console.WriteLine($"First Response: {firstResult!["Response"]}");

        var secondMessage = new DeepSeekRequest
        {
            Prompt = "What is my name?"
        };

        var secondResponse = await _client.PostAsJsonAsync("/deepseek/chat", secondMessage, TestContext.Current.CancellationToken);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        secondResult.Should().NotBeNull();
        
        Console.WriteLine($"Second Response: {secondResult!["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithLowTemperature_ProducesDeterministicOutput()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "What is 2 + 2?",
            Options = new DeepSeekOptions
            {
                Temperature = 0.0
            }
        };

        var firstResponse = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);

        await Task.Delay(1000);

        var secondResponse = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);

        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();
        firstResult!["Response"].Should().Contain("4");
        secondResult!["Response"].Should().Contain("4");
        
        Console.WriteLine($"Deterministic Response 1: {firstResult["Response"]}");
        Console.WriteLine($"Deterministic Response 2: {secondResult["Response"]}");
    }
}
