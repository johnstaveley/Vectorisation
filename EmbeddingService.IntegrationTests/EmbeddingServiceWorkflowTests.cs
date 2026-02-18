using EmbeddingService.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace EmbeddingService.IntegrationTests;

public class EmbeddingServiceWorkflowTests
{
    private readonly HttpClient _client;

    public EmbeddingServiceWorkflowTests()
    {
        _client = new DefaultHttpClientFactory().CreateClient();
    }

    [Fact]
    public async Task CompleteWorkflow_CreateSearchAndDelete_WorksCorrectly()
    {
        var documents = new[]
        {
            "The quick brown fox jumps over the lazy dog",
            "Artificial intelligence is transforming the world",
            "Machine learning requires large datasets",
            "Natural language processing enables computers to understand text"
        };

        var createdIds = new List<string>();

        foreach (var doc in documents)
        {
            var createRequest = new EmbeddingRequest
            {
                Text = doc,
                Metadata = new Dictionary<string, string>
                {
                    { "workflow-test", "true" },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                }
            };

            var createResponse = await _client.PostAsJsonAsync("/embeddings", createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
            embeddingResponse.Should().NotBeNull();
            createdIds.Add(embeddingResponse!.Id);
        }

        await Task.Delay(1500);

        var searchRequest = new SearchRequest
        {
            Query = "AI and machine learning",
            TopK = 3
        };

        var searchResponse = await _client.PostAsJsonAsync("/search", searchRequest);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await searchResponse.Content.ReadFromJsonAsync<List<SearchResult>>();
        searchResults.Should().NotBeNull();
        searchResults!.Should().NotBeEmpty();
        searchResults.Should().HaveCountLessThanOrEqualTo(3);

        foreach (var id in createdIds)
        {
            var deleteResponse = await _client.DeleteAsync($"/embeddings/{id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        foreach (var id in createdIds)
        {
            var getResponse = await _client.GetAsync($"/embeddings/{id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task SearchRelevance_ReturnsMoreRelevantResultsFirst()
    {
        var embeddingRequest1 = new EmbeddingRequest
        {
            Text = "Python is a programming language for data science and machine learning"
        };
        var embeddingRequest2 = new EmbeddingRequest
        {
            Text = "JavaScript is used for web development"
        };
        var embeddingRequest3 = new EmbeddingRequest
        {
            Text = "Data science involves analyzing and interpreting complex data"
        };

        await _client.PostAsJsonAsync("/embeddings", embeddingRequest1);
        await _client.PostAsJsonAsync("/embeddings", embeddingRequest2);
        await _client.PostAsJsonAsync("/embeddings", embeddingRequest3);

        await Task.Delay(1500);

        var searchRequest = new SearchRequest
        {
            Query = "Tell me about data science",
            TopK = 3
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest);
        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>();

        results.Should().NotBeNull();
        results!.Should().NotBeEmpty();

        if (results.Count > 1)
        {
            for (int i = 0; i < results.Count - 1; i++)
            {
                results[i].Score.Should().BeGreaterThanOrEqualTo(results[i + 1].Score, 
                    "Results should be ordered by score descending");
            }
        }
    }

    [Fact]
    public async Task ConcurrentRequests_HandleMultipleEmbeddingCreations()
    {
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            var request = new EmbeddingRequest
            {
                Text = $"Concurrent test document number {i}",
                Metadata = new Dictionary<string, string>
                {
                    { "batch", "concurrent-test" },
                    { "index", i.ToString() }
                }
            };

            var response = await _client.PostAsJsonAsync("/embeddings", request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
            embeddingResponse.Should().NotBeNull();
            return embeddingResponse!;
        });

        var results = await Task.WhenAll(tasks);

        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r => r.Id.Should().NotBeEmpty());
        results.Select(r => r.Id).Distinct().Should().HaveCount(5);
    }

    [Fact]
    public async Task UpdateWorkflow_DeleteAndRecreateDocument()
    {
        var originalRequest = new EmbeddingRequest
        {
            Text = "Original document text",
            Metadata = new Dictionary<string, string> { { "version", "1" } }
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", originalRequest);
        var originalEmbedding = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
        originalEmbedding.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync($"/embeddings/{originalEmbedding!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updatedRequest = new EmbeddingRequest
        {
            Text = "Updated document text",
            Metadata = new Dictionary<string, string> { { "version", "2" } }
        };

        var updateCreateResponse = await _client.PostAsJsonAsync("/embeddings", updatedRequest);
        var updatedEmbedding = await updateCreateResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
        updatedEmbedding.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/embeddings/{updatedEmbedding!.Id}");
        var document = await getResponse.Content.ReadFromJsonAsync<EmbeddingDocument>();

        document.Should().NotBeNull();
        document!.Text.Should().Be("Updated document text");
        document.Metadata.Should().NotBeNull();
        document.Metadata!["version"].Should().Be("2");
    }
}
