using EmbeddingService.Models;
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
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
            Assert.NotNull(embeddingResponse);
            createdIds.Add(embeddingResponse.Id);
        }

        await Task.Delay(1500);

        var searchRequest = new SearchRequest
        {
            Query = "AI and machine learning",
            TopK = 3
        };

        var searchResponse = await _client.PostAsJsonAsync("/search", searchRequest);
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

        var searchResults = await searchResponse.Content.ReadFromJsonAsync<List<SearchResult>>();
        Assert.NotNull(searchResults);
        Assert.True(searchResults.Count > 0);
        Assert.True(searchResults.Count <= 3);

        foreach (var id in createdIds)
        {
            var deleteResponse = await _client.DeleteAsync($"/embeddings/{id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        foreach (var id in createdIds)
        {
            var getResponse = await _client.GetAsync($"/embeddings/{id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
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

        Assert.NotNull(results);
        Assert.True(results.Count > 0);
        
        if (results.Count > 1)
        {
            for (int i = 0; i < results.Count - 1; i++)
            {
                Assert.True(results[i].Score >= results[i + 1].Score, 
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
            Assert.NotNull(embeddingResponse);
            return embeddingResponse;
        });

        var results = await Task.WhenAll(tasks);

        Assert.Equal(5, results.Length);
        Assert.All(results, r => Assert.NotEmpty(r.Id));
        Assert.Equal(5, results.Select(r => r.Id).Distinct().Count());
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
        Assert.NotNull(originalEmbedding);

        var deleteResponse = await _client.DeleteAsync($"/embeddings/{originalEmbedding.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var updatedRequest = new EmbeddingRequest
        {
            Text = "Updated document text",
            Metadata = new Dictionary<string, string> { { "version", "2" } }
        };

        var updateCreateResponse = await _client.PostAsJsonAsync("/embeddings", updatedRequest);
        var updatedEmbedding = await updateCreateResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
        Assert.NotNull(updatedEmbedding);

        var getResponse = await _client.GetAsync($"/embeddings/{updatedEmbedding.Id}");
        var document = await getResponse.Content.ReadFromJsonAsync<EmbeddingDocument>();
        
        Assert.NotNull(document);
        Assert.Equal("Updated document text", document.Text);
        Assert.Equal("2", document.Metadata?["version"]);
    }
}
