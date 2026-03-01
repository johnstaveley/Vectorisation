using EmbeddingService.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace EmbeddingService.IntegrationTests;

[Collection("Integration Tests")]
public class DeepSeekServiceEdgeCaseTests
{
    private readonly HttpClient _client;

    public DeepSeekServiceEdgeCaseTests()
    {
        _client = new DefaultHttpClientFactory().CreateClient();
    }

    [Fact]
    public async Task DeepSeekGenerate_WithSpecialCharacters_HandlesCorrectly()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "Explain this symbol: @#$%^&*() and these quotes: \"Hello\" 'World'"
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Special Characters Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithUnicodeCharacters_HandlesCorrectly()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "Translate 'Hello' to: Chinese (你好), Arabic (مرحبا), Japanese (こんにちは), Russian (Привет)"
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Unicode Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithMultilinePrompt_HandlesCorrectly()
    {
        var request = new DeepSeekRequest
        {
            Prompt = @"Answer these questions:
1. What is AI?
2. What is ML?
3. What is the difference?"
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Multiline Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithVeryHighTemperature_StillReturnsResponse()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "Tell me a creative story about a robot.",
            Options = new DeepSeekOptions
            {
                Temperature = 1.5
            }
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"High Temperature Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithVeryLowMaxTokens_ReturnsShortResponse()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "Explain quantum computing in great detail with examples.",
            Options = new DeepSeekOptions
            {
                MaxTokens = 50
            }
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        result["Response"].Length.Should().BeLessThan(500, "response should be truncated due to low max tokens");
        
        Console.WriteLine($"Short Response ({result["Response"].Length} chars): {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekChat_WithEmoji_HandlesCorrectly()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "What do these emojis mean? 😊🎉🚀💻🌟"
        };

        var response = await _client.PostAsJsonAsync("/deepseek/chat", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Emoji Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithJsonRequest_HandlesCorrectly()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "Create a JSON object representing a user with name, age, and email fields."
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        result["Response"].Should().Contain("{", "response should contain JSON");
        
        Console.WriteLine($"JSON Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithMathematicalExpression_CalculatesCorrectly()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "What is 123 * 456 + 789?",
            Options = new DeepSeekOptions
            {
                Temperature = 0.0
            }
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        result["Response"].Should().Contain("56877", "should contain the correct answer");
        
        Console.WriteLine($"Math Response: {result["Response"]}");
    }

    [Fact]
    public async Task DeepSeekGenerate_WithRepeatedPrompts_ProducesConsistentResults()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "What is the capital of Japan?",
            Options = new DeepSeekOptions
            {
                Temperature = 0.0
            }
        };

        var responses = new List<string>();

        for (int i = 0; i < 3; i++)
        {
            var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
            
            responses.Add(result!["Response"]);
            await Task.Delay(500);
        }

        responses.Should().AllSatisfy(r => r.Should().Contain("Tokyo"));
        
        Console.WriteLine("Consistency Check:");
        for (int i = 0; i < responses.Count; i++)
        {
            Console.WriteLine($"Response {i + 1}: {responses[i]}");
        }
    }

    [Fact]
    public async Task DeepSeekGenerate_WithWhitespaceOnlyPrompt_ReturnsBadRequest()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "   \t\n   "
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeepSeekGenerate_WithSQLQuery_HandlesCorrectly()
    {
        var request = new DeepSeekRequest
        {
            Prompt = "Write a SQL query to select all users from a 'users' table where age is greater than 18."
        };

        var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["Response"].Should().NotBeNullOrWhiteSpace();
        result["Response"].Should().Contain("SELECT", "response should contain SQL");
        
        Console.WriteLine($"SQL Response: {result["Response"]}");
    }
}
