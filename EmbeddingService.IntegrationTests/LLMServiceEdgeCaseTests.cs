using EmbeddingService.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace EmbeddingService.IntegrationTests;

[Collection("Integration Tests")]
public class LLMServiceEdgeCaseTests
{
    private readonly HttpClient _client;

    public LLMServiceEdgeCaseTests()
    {
        _client = new DefaultHttpClientFactory().CreateClient();
        _client.Timeout = TimeSpan.FromMinutes(5);
    }

    [Fact]
    public async Task LLMGenerate_WithSpecialCharacters_HandlesCorrectly()
    {
        var request = new LLMRequest
        {
            Prompt = "Explain this symbol: @#$%^&*() and these quotes: \"Hello\" 'World'"
        };

        var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Special Characters Response: {result["response"]}");
    }

    [Fact]
    public async Task LLMGenerate_WithUnicodeCharacters_HandlesCorrectly()
    {
        var request = new LLMRequest
        {
            Prompt = "Translate 'Hello' to: Chinese (你好), Arabic (مرحبا), Japanese (こんにちは), Russian (Привет)"
        };

        var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Unicode Response: {result["response"]}");
    }

    [Fact]
    public async Task LLMGenerate_WithMultilinePrompt_HandlesCorrectly()
    {
        var request = new LLMRequest
        {
            Prompt = @"Answer these questions:
1. What is AI?
2. What is ML?
3. What is the difference?"
        };

        var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Multiline Response: {result["response"]}");
    }

    [Fact]
    public async Task LLMGenerate_WithVeryHighTemperature_StillReturnsResponse()
    {
        var request = new LLMRequest
        {
            Prompt = "Tell me a creative story about a robot.",
            Options = new LLMOptions
            {
                Temperature = 1.5
            }
        };

        var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"High Temperature Response: {result["response"]}");
    }

    [Fact]
    public async Task LLMGenerate_WithVeryLowMaxTokens_ReturnsShortResponse()
    {
        var request = new LLMRequest
        {
            Prompt = "Explain quantum computing in great detail with examples.",
            Options = new LLMOptions
            {
                MaxTokens = 50
            }
        };

        var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["response"].Should().NotBeNullOrWhiteSpace();
        result["response"].Length.Should().BeLessThan(500, "response should be truncated due to low max tokens");
        
        Console.WriteLine($"Short Response ({result["response"].Length} chars): {result["response"]}");
    }

    [Fact]
    public async Task LLMChat_WithEmoji_HandlesCorrectly()
    {
        var request = new LLMRequest
        {
            Prompt = "What do these emojis mean? 😊🎉🚀💻🌟"
        };

        var response = await _client.PostAsJsonAsync("/LLM/chat", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["response"].Should().NotBeNullOrWhiteSpace();
        
        Console.WriteLine($"Emoji Response: {result["response"]}");
    }

    [Fact]
    public async Task LLMGenerate_WithJsonRequest_HandlesCorrectly()
    {
        var request = new LLMRequest
        {
            Prompt = "Create a JSON object representing a user with name, age, and email fields."
        };

        var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["response"].Should().NotBeNullOrWhiteSpace();
        result["response"].Should().Contain("{", "response should contain JSON");
        
        Console.WriteLine($"JSON Response: {result["response"]}");
    }

    [Fact]
    public async Task LLMGenerate_WithMathematicalExpression_CalculatesCorrectly()
    {
        var request = new LLMRequest
        {
            Prompt = "What is 123 * 456 + 789?",
            Options = new LLMOptions
            {
                Temperature = 0.0
            }
        };

        var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["response"].Should().NotBeNullOrWhiteSpace();
        result["response"].Replace(",", "").Should().Contain("56877", "should contain the correct answer");
        
        Console.WriteLine($"Math Response: {result["response"]}");
    }

    [Fact]
    public async Task LLMGenerate_WithRepeatedPrompts_ProducesConsistentResults()
    {
        var request = new LLMRequest
        {
            Prompt = "What is the capital of Japan?",
            Options = new LLMOptions
            {
                Temperature = 0.0
            }
        };

        var responses = new List<string>();

        for (int i = 0; i < 3; i++)
        {
            var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
            
            responses.Add(result!["response"]);
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        responses.Should().AllSatisfy(r => r.Should().Contain("Tokyo"));
        
        Console.WriteLine("Consistency Check:");
        for (int i = 0; i < responses.Count; i++)
        {
            Console.WriteLine($"Response {i + 1}: {responses[i]}");
        }
    }

    [Fact]
    public async Task LLMGenerate_WithWhitespaceOnlyPrompt_ReturnsBadRequest()
    {
        var request = new LLMRequest
        {
            Prompt = "   \t\n   "
        };

        var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LLMGenerate_WithSQLQuery_HandlesCorrectly()
    {
        var request = new LLMRequest
        {
            Prompt = "Write a SQL query to select all users from a 'users' table where age is greater than 18."
        };

        var response = await _client.PostAsJsonAsync("/LLM/generate", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!["response"].Should().NotBeNullOrWhiteSpace();
        result["response"].Should().Contain("SELECT", "response should contain SQL");
        
        Console.WriteLine($"SQL Response: {result["response"]}");
    }
}
