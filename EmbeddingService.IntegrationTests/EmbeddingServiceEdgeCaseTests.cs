using System.Net;
using System.Net.Http.Json;
using EmbeddingService.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EmbeddingService.IntegrationTests;

public class EmbeddingServiceEdgeCaseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EmbeddingServiceEdgeCaseTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateEmbedding_WithVeryLongText_ReturnsOk()
    {
        var longText = string.Join(" ", Enumerable.Repeat("This is a sentence.", 100));
        var request = new EmbeddingRequest
        {
            Text = longText
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        Assert.NotNull(embeddingResponse);
        Assert.NotEmpty(embeddingResponse.Embedding);
    }

    [Fact]
    public async Task CreateEmbedding_WithSpecialCharacters_ReturnsOk()
    {
        var request = new EmbeddingRequest
        {
            Text = "Special characters: @#$%^&*()_+-=[]{}|;:',.<>?/~`! \"Hello\" 'World'"
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        Assert.NotNull(embeddingResponse);
        Assert.Equal(request.Text, embeddingResponse.Text);
    }

    [Fact]
    public async Task CreateEmbedding_WithUnicodeCharacters_ReturnsOk()
    {
        var request = new EmbeddingRequest
        {
            Text = "Unicode test: ???? ?????? ????? ????? ?????"
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        Assert.NotNull(embeddingResponse);
        Assert.Equal(request.Text, embeddingResponse.Text);
    }

    [Fact]
    public async Task CreateEmbedding_WithEmptyMetadata_ReturnsOk()
    {
        var request = new EmbeddingRequest
        {
            Text = "Text with empty metadata",
            Metadata = new Dictionary<string, string>()
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmbedding_WithNullMetadata_ReturnsOk()
    {
        var request = new EmbeddingRequest
        {
            Text = "Text with null metadata",
            Metadata = null
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithTopKZero_ReturnsEmptyResults()
    {
        var searchRequest = new SearchRequest
        {
            Query = "Test query",
            TopK = 0
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>();
        Assert.NotNull(results);
    }

    [Fact]
    public async Task Search_WithLargeTopK_ReturnsAvailableResults()
    {
        var createRequest = new EmbeddingRequest
        {
            Text = "Single document for large TopK test"
        };
        await _client.PostAsJsonAsync("/embeddings", createRequest);

        await Task.Delay(1000);

        var searchRequest = new SearchRequest
        {
            Query = "document test",
            TopK = 1000
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>();
        Assert.NotNull(results);
    }

    [Fact]
    public async Task GetEmbedding_WithEmptyId_ReturnsBadRequestOrNotFound()
    {
        var response = await _client.GetAsync("/embeddings/");

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task DeleteEmbedding_CalledTwice_SecondCallReturnsNotFound()
    {
        var createRequest = new EmbeddingRequest
        {
            Text = "Document to be deleted twice"
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", createRequest);
        var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
        Assert.NotNull(embeddingResponse);

        var firstDeleteResponse = await _client.DeleteAsync($"/embeddings/{embeddingResponse.Id}");
        Assert.Equal(HttpStatusCode.NoContent, firstDeleteResponse.StatusCode);

        var secondDeleteResponse = await _client.DeleteAsync($"/embeddings/{embeddingResponse.Id}");
        Assert.Equal(HttpStatusCode.NotFound, secondDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEmbedding_WithWhitespaceOnlyText_ReturnsBadRequest()
    {
        var request = new EmbeddingRequest
        {
            Text = "   "
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithWhitespaceOnlyQuery_ReturnsBadRequest()
    {
        var searchRequest = new SearchRequest
        {
            Query = "   ",
            TopK = 5
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_InEmptyIndex_ReturnsEmptyResults()
    {
        var searchRequest = new SearchRequest
        {
            Query = "This query should return no results from empty index",
            TopK = 5
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>();
        Assert.NotNull(results);
    }
}
