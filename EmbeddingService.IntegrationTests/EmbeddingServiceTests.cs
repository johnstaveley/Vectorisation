using System.Net;
using System.Net.Http.Json;
using EmbeddingService.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EmbeddingService.IntegrationTests;

public class EmbeddingServiceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EmbeddingServiceTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateEmbedding_WithValidText_ReturnsOkWithEmbedding()
    {
        var request = new EmbeddingRequest
        {
            Text = "This is a test sentence for embedding generation.",
            Metadata = new Dictionary<string, string>
            {
                { "source", "integration-test" },
                { "category", "test" }
            }
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        Assert.NotNull(embeddingResponse);
        Assert.NotEmpty(embeddingResponse.Id);
        Assert.NotEmpty(embeddingResponse.Embedding);
        Assert.Equal(request.Text, embeddingResponse.Text);
    }

    [Fact]
    public async Task CreateEmbedding_WithEmptyText_ReturnsBadRequest()
    {
        var request = new EmbeddingRequest
        {
            Text = ""
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmbedding_WithNullText_ReturnsBadRequest()
    {
        var request = new EmbeddingRequest
        {
            Text = null!
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEmbedding_WithValidId_ReturnsDocument()
    {
        var createRequest = new EmbeddingRequest
        {
            Text = "Test document for retrieval"
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", createRequest);
        var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
        Assert.NotNull(embeddingResponse);

        var getResponse = await _client.GetAsync($"/embeddings/{embeddingResponse.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        
        var document = await getResponse.Content.ReadFromJsonAsync<EmbeddingDocument>();
        Assert.NotNull(document);
        Assert.Equal(createRequest.Text, document.Text);
        Assert.NotEmpty(document.Embedding);
    }

    [Fact]
    public async Task GetEmbedding_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/embeddings/nonexistent-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SearchSimilar_WithValidQuery_ReturnsResults()
    {
        var embeddingRequest1 = new EmbeddingRequest
        {
            Text = "Machine learning is a subset of artificial intelligence"
        };
        var embeddingRequest2 = new EmbeddingRequest
        {
            Text = "Deep learning uses neural networks"
        };
        var embeddingRequest3 = new EmbeddingRequest
        {
            Text = "The weather is nice today"
        };

        await _client.PostAsJsonAsync("/embeddings", embeddingRequest1);
        await _client.PostAsJsonAsync("/embeddings", embeddingRequest2);
        await _client.PostAsJsonAsync("/embeddings", embeddingRequest3);

        await Task.Delay(1000);

        var searchRequest = new SearchRequest
        {
            Query = "What is artificial intelligence?",
            TopK = 2
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>();
        Assert.NotNull(results);
        Assert.True(results.Count <= 2);
        
        if (results.Count > 0)
        {
            Assert.NotEmpty(results[0].Id);
            Assert.NotEmpty(results[0].Text);
        }
    }

    [Fact]
    public async Task SearchSimilar_WithEmptyQuery_ReturnsBadRequest()
    {
        var searchRequest = new SearchRequest
        {
            Query = "",
            TopK = 5
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchSimilar_WithNullQuery_ReturnsBadRequest()
    {
        var searchRequest = new SearchRequest
        {
            Query = null!,
            TopK = 5
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmbedding_WithValidId_ReturnsNoContent()
    {
        var createRequest = new EmbeddingRequest
        {
            Text = "Document to be deleted"
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", createRequest);
        var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
        Assert.NotNull(embeddingResponse);

        var deleteResponse = await _client.DeleteAsync($"/embeddings/{embeddingResponse.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/embeddings/{embeddingResponse.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteEmbedding_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/embeddings/nonexistent-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmbedding_WithMetadata_PreservesMetadata()
    {
        var request = new EmbeddingRequest
        {
            Text = "Document with metadata",
            Metadata = new Dictionary<string, string>
            {
                { "author", "John Doe" },
                { "category", "technology" },
                { "date", "2024-01-01" }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", request);
        var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
        Assert.NotNull(embeddingResponse);

        var getResponse = await _client.GetAsync($"/embeddings/{embeddingResponse.Id}");
        var document = await getResponse.Content.ReadFromJsonAsync<EmbeddingDocument>();
        
        Assert.NotNull(document);
        Assert.NotNull(document.Metadata);
        Assert.Equal(3, document.Metadata.Count);
        Assert.Equal("John Doe", document.Metadata["author"]);
        Assert.Equal("technology", document.Metadata["category"]);
        Assert.Equal("2024-01-01", document.Metadata["date"]);
    }
}
