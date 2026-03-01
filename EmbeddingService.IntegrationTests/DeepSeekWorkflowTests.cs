using EmbeddingService.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace EmbeddingService.IntegrationTests;

[Collection("Integration Tests")]
public class DeepSeekWorkflowTests
{
    private readonly HttpClient _client;

    public DeepSeekWorkflowTests()
    {
        _client = new DefaultHttpClientFactory().CreateClient();
    }

    [Fact]
    public async Task Workflow_GenerateTextWithDeepSeek_ThenCreateEmbedding()
    {
        Console.WriteLine("=== Workflow: Generate Text + Create Embedding ===");

        var deepSeekRequest = new DeepSeekRequest
        {
            Prompt = "Write a short poem about artificial intelligence.",
            Options = new DeepSeekOptions
            {
                MaxTokens = 200
            }
        };

        Console.WriteLine("Step 1: Generating poem with DeepSeek...");
        var deepSeekResponse = await _client.PostAsJsonAsync("/deepseek/generate", deepSeekRequest, TestContext.Current.CancellationToken);
        deepSeekResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deepSeekResult = await deepSeekResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        deepSeekResult.Should().NotBeNull();
        var generatedPoem = deepSeekResult!["Response"];
        
        Console.WriteLine($"Generated Poem:\n{generatedPoem}\n");

        Console.WriteLine("Step 2: Creating embedding for generated poem...");
        var embeddingRequest = new EmbeddingRequest
        {
            Text = generatedPoem,
            Metadata = new Dictionary<string, string>
            {
                { "source", "deepseek-generated" },
                { "type", "poem" },
                { "topic", "AI" }
            }
        };

        var embeddingResponse = await _client.PostAsJsonAsync("/embeddings", embeddingRequest, TestContext.Current.CancellationToken);
        embeddingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var embeddingResult = await embeddingResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        embeddingResult.Should().NotBeNull();
        embeddingResult!.Embedding.Should().NotBeEmpty();
        
        Console.WriteLine($"Embedding created with ID: {embeddingResult.Id}");
        Console.WriteLine($"Embedding dimensions: {embeddingResult.Embedding.Length}");
    }

    [Fact]
    public async Task Workflow_DeepSeekSummarizesSearchResults()
    {
        Console.WriteLine("=== Workflow: Search + Summarize with DeepSeek ===");

        Console.WriteLine("Step 1: Creating sample documents...");
        var documents = new[]
        {
            "Machine learning is a subset of artificial intelligence.",
            "Deep learning uses neural networks with multiple layers.",
            "Natural language processing helps computers understand human language."
        };

        foreach (var doc in documents)
        {
            var embeddingRequest = new EmbeddingRequest { Text = doc };
            await _client.PostAsJsonAsync("/embeddings", embeddingRequest, TestContext.Current.CancellationToken);
            await Task.Delay(500);
        }

        Console.WriteLine("Step 2: Searching for similar documents...");
        var searchRequest = new SearchRequest
        {
            Query = "Tell me about AI and machine learning",
            TopK = 3
        };

        var searchResponse = await _client.PostAsJsonAsync("/search", searchRequest, TestContext.Current.CancellationToken);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await searchResponse.Content.ReadFromJsonAsync<List<SearchResult>>(cancellationToken: TestContext.Current.CancellationToken);
        searchResults.Should().NotBeNull();
        searchResults.Should().NotBeEmpty();

        Console.WriteLine($"Found {searchResults!.Count} results");

        Console.WriteLine("Step 3: Using DeepSeek to summarize results...");
        var combinedText = string.Join("\n", searchResults.Select(r => r.Text));
        var summaryRequest = new DeepSeekRequest
        {
            Prompt = $"Summarize these points concisely:\n{combinedText}"
        };

        var summaryResponse = await _client.PostAsJsonAsync("/deepseek/generate", summaryRequest, TestContext.Current.CancellationToken);
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var summaryResult = await summaryResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        summaryResult.Should().NotBeNull();
        
        Console.WriteLine($"Summary: {summaryResult!["Response"]}");
    }

    [Fact]
    public async Task Workflow_DeepSeekGeneratesQuestions_SystemAnswers()
    {
        Console.WriteLine("=== Workflow: DeepSeek Q&A Generation ===");

        Console.WriteLine("Step 1: Generate questions about a topic...");
        var questionRequest = new DeepSeekRequest
        {
            Prompt = "Generate 3 simple questions about the solar system.",
            Options = new DeepSeekOptions
            {
                Temperature = 0.7,
                MaxTokens = 300
            }
        };

        var questionResponse = await _client.PostAsJsonAsync("/deepseek/generate", questionRequest, TestContext.Current.CancellationToken);
        var questionResult = await questionResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        
        var questions = questionResult!["Response"];
        Console.WriteLine($"Generated Questions:\n{questions}\n");

        Console.WriteLine("Step 2: Answer the first question...");
        var answerRequest = new DeepSeekRequest
        {
            Prompt = $"Answer this question briefly: What is the largest planet in our solar system?",
            Options = new DeepSeekOptions
            {
                Temperature = 0.3
            }
        };

        var answerResponse = await _client.PostAsJsonAsync("/deepseek/generate", answerRequest, TestContext.Current.CancellationToken);
        answerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var answerResult = await answerResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        answerResult.Should().NotBeNull();
        answerResult!["Response"].Should().Contain("Jupiter");
        
        Console.WriteLine($"Answer: {answerResult["Response"]}");
    }

    [Fact]
    public async Task Workflow_CompareDeepSeekTemperatures()
    {
        Console.WriteLine("=== Workflow: Temperature Comparison ===");

        var basePrompt = "Complete this sentence: The future of technology is";

        var temperatures = new[] { 0.0, 0.5, 1.0, 1.5 };
        
        foreach (var temp in temperatures)
        {
            var request = new DeepSeekRequest
            {
                Prompt = basePrompt,
                Options = new DeepSeekOptions
                {
                    Temperature = temp,
                    MaxTokens = 100
                }
            };

            var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
            
            Console.WriteLine($"\nTemperature {temp}:");
            Console.WriteLine(result!["Response"]);
            
            await Task.Delay(1000);
        }
    }

    [Fact]
    public async Task Workflow_DeepSeekGeneratesCodeThenExplains()
    {
        Console.WriteLine("=== Workflow: Code Generation + Explanation ===");

        Console.WriteLine("Step 1: Generate code...");
        var codeRequest = new DeepSeekRequest
        {
            Prompt = "Write a simple C# function to check if a number is prime.",
            Options = new DeepSeekOptions
            {
                Temperature = 0.3
            }
        };

        var codeResponse = await _client.PostAsJsonAsync("/deepseek/generate", codeRequest, TestContext.Current.CancellationToken);
        var codeResult = await codeResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        
        var generatedCode = codeResult!["Response"];
        Console.WriteLine($"Generated Code:\n{generatedCode}\n");

        Console.WriteLine("Step 2: Explain the code...");
        var explainRequest = new DeepSeekRequest
        {
            Prompt = $"Explain this code in simple terms:\n{generatedCode}"
        };

        var explainResponse = await _client.PostAsJsonAsync("/deepseek/generate", explainRequest, TestContext.Current.CancellationToken);
        explainResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var explainResult = await explainResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
        explainResult.Should().NotBeNull();
        
        Console.WriteLine($"Explanation:\n{explainResult!["Response"]}");
    }

    [Fact]
    public async Task Workflow_MultipleModelRequests_InParallel()
    {
        Console.WriteLine("=== Workflow: Parallel Requests ===");

        var prompts = new[]
        {
            "What is 5 + 5?",
            "Name a color.",
            "What day comes after Monday?"
        };

        var tasks = prompts.Select(async prompt =>
        {
            var request = new DeepSeekRequest
            {
                Prompt = prompt,
                Options = new DeepSeekOptions { Temperature = 0.0 }
            };

            var response = await _client.PostAsJsonAsync("/deepseek/generate", request, TestContext.Current.CancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: TestContext.Current.CancellationToken);
            
            return new { Prompt = prompt, Response = result!["Response"] };
        });

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(3);
        foreach (var result in results)
        {
            result.Response.Should().NotBeNullOrWhiteSpace();
            Console.WriteLine($"Q: {result.Prompt}");
            Console.WriteLine($"A: {result.Response}\n");
        }
    }
}
