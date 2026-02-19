using EmbeddingService.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace EmbeddingService.IntegrationTests;

[Collection("Integration Tests")]
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
        // Clean out existing data
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        var deleteAllResponse = await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);
        deleteAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

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

            var createResponse = await _client.PostAsJsonAsync("/embeddings", createRequest, cancellationToken: TestContext.Current.CancellationToken);
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
            embeddingResponse.Should().NotBeNull();
            createdIds.Add(embeddingResponse!.Id);
        }

        await Task.Delay(1500, cancellationToken: TestContext.Current.CancellationToken);

        var searchRequest = new SearchRequest
        {
            Query = "AI and machine learning",
            TopK = 3
        };

        var searchResponse = await _client.PostAsJsonAsync("/search", searchRequest, cancellationToken: TestContext.Current.CancellationToken);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await searchResponse.Content.ReadFromJsonAsync<List<SearchResult>>(cancellationToken: TestContext.Current.CancellationToken);
        searchResults.Should().NotBeNull();
        searchResults!.Should().NotBeEmpty();
        searchResults.Should().HaveCountLessThanOrEqualTo(3);

        foreach (var id in createdIds)
        {
            var deleteResponse = await _client.DeleteAsync($"/embeddings/{id}", cancellationToken: TestContext.Current.CancellationToken);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        foreach (var id in createdIds)
        {
            var getResponse = await _client.GetAsync($"/embeddings/{id}", cancellationToken: TestContext.Current.CancellationToken);
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task SearchRelevance_ReturnsMoreRelevantResultsFirst()
    {
        // Clean out existing data
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        var deleteAllResponse = await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);
        deleteAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

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

        await _client.PostAsJsonAsync("/embeddings", embeddingRequest1, cancellationToken: TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/embeddings", embeddingRequest2, cancellationToken: TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/embeddings", embeddingRequest3, cancellationToken: TestContext.Current.CancellationToken);

        await Task.Delay(1500, cancellationToken: TestContext.Current.CancellationToken);

        var searchRequest = new SearchRequest
        {
            Query = "Tell me about data science",
            TopK = 3
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest, cancellationToken: TestContext.Current.CancellationToken);
        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>(cancellationToken: TestContext.Current.CancellationToken);

        results.Should().NotBeNull();
        results.Should().NotBeEmpty();

        if (results.Count > 1)
        {
            for (int i = 0; i < results.Count - 1; i++)
            {
                results[i].Score.Should().BeGreaterThanOrEqualTo(results[i + 1].Score, "Results should be ordered by score descending");
            }
        }
        results[0].Text.Should().Be(embeddingRequest3.Text);
        results[1].Text.Should().Be(embeddingRequest1.Text);
        results[2].Text.Should().Be(embeddingRequest2.Text);
    }

    [Fact]
    public async Task ConcurrentRequests_HandleMultipleEmbeddingCreations()
    {
        // Clean out existing data
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        var deleteAllResponse = await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);
        deleteAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

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

            var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);
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
        // Clean out existing data
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        var deleteAllResponse = await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);
        deleteAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        var originalRequest = new EmbeddingRequest
        {
            Text = "Original document text",
            Metadata = new Dictionary<string, string> { { "version", "1" } }
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", originalRequest, cancellationToken: TestContext.Current.CancellationToken);
        var originalEmbedding = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        originalEmbedding.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync($"/embeddings/{originalEmbedding!.Id}", cancellationToken: TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updatedRequest = new EmbeddingRequest
        {
            Text = "Updated document text",
            Metadata = new Dictionary<string, string> { { "version", "2" } }
        };

        var updateCreateResponse = await _client.PostAsJsonAsync("/embeddings", updatedRequest, cancellationToken: TestContext.Current.CancellationToken);
        var updatedEmbedding = await updateCreateResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        updatedEmbedding.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/embeddings/{updatedEmbedding!.Id}", cancellationToken: TestContext.Current.CancellationToken);
        var document = await getResponse.Content.ReadFromJsonAsync<EmbeddingDocument>(cancellationToken: TestContext.Current.CancellationToken);

        document.Should().NotBeNull();
        document!.Text.Should().Be("Updated document text");
        document.Metadata.Should().NotBeNull();
        document.Metadata!["version"].Should().Be("2");
    }
}
