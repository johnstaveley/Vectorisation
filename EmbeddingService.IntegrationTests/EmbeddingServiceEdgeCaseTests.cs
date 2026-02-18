using EmbeddingService.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace EmbeddingService.IntegrationTests;

public class EmbeddingServiceEdgeCaseTests
{
    private readonly HttpClient _client;

    public EmbeddingServiceEdgeCaseTests()
    {
        _client = new DefaultHttpClientFactory().CreateClient();
    }

    [Fact]
    public async Task CreateEmbedding_WithVeryLongText_ReturnsOk()
    {
        var longText = string.Join(" ", Enumerable.Repeat("This is a sentence.", 100));
        var request = new EmbeddingRequest
        {
            Text = longText
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        embeddingResponse.Should().NotBeNull();
        embeddingResponse!.Embedding.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateEmbedding_WithSpecialCharacters_ReturnsOk()
    {
        var request = new EmbeddingRequest
        {
            Text = "Special characters: @#$%^&*()_+-=[]{}|;:',.<>?/~`! \"Hello\" 'World'"
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        embeddingResponse.Should().NotBeNull();
        embeddingResponse!.Text.Should().Be(request.Text);
    }

    [Fact]
    public async Task CreateEmbedding_WithUnicodeCharacters_ReturnsOk()
    {
        var request = new EmbeddingRequest
        {
            Text = "Unicode test: ???? ?????? ????? ????? ?????"
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        embeddingResponse.Should().NotBeNull();
        embeddingResponse!.Text.Should().Be(request.Text);
    }

    [Fact]
    public async Task CreateEmbedding_WithEmptyMetadata_ReturnsOk()
    {
        var request = new EmbeddingRequest
        {
            Text = "Text with empty metadata",
            Metadata = new Dictionary<string, string>()
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateEmbedding_WithNullMetadata_ReturnsOk()
    {
        var request = new EmbeddingRequest
        {
            Text = "Text with null metadata",
            Metadata = null
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Search_WithTopKZero_ReturnsEmptyResults()
    {
        var searchRequest = new SearchRequest
        {
            Query = "Test query",
            TopK = 0
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>(cancellationToken: TestContext.Current.CancellationToken);
        results.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_WithLargeTopK_ReturnsAvailableResults()
    {
        var createRequest = new EmbeddingRequest
        {
            Text = "Single document for large TopK test"
        };
        await _client.PostAsJsonAsync("/embeddings", createRequest, cancellationToken: TestContext.Current.CancellationToken);

        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var searchRequest = new SearchRequest
        {
            Query = "document test",
            TopK = 1000
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>(cancellationToken: TestContext.Current.CancellationToken);
        results.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEmbedding_WithEmptyId_ReturnsBadRequestOrNotFound()
    {
        var response = await _client.GetAsync("/embeddings/", cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest,
            HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task DeleteEmbedding_CalledTwice_SecondCallReturnsNotFound()
    {
        var createRequest = new EmbeddingRequest
        {
            Text = "Document to be deleted twice"
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", createRequest, cancellationToken: TestContext.Current.CancellationToken);
        var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        embeddingResponse.Should().NotBeNull();

        var firstDeleteResponse = await _client.DeleteAsync($"/embeddings/{embeddingResponse!.Id}", cancellationToken: TestContext.Current.CancellationToken);
        firstDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondDeleteResponse = await _client.DeleteAsync($"/embeddings/{embeddingResponse.Id}", cancellationToken: TestContext.Current.CancellationToken);
        secondDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateEmbedding_WithWhitespaceOnlyText_ReturnsBadRequest()
    {
        var request = new EmbeddingRequest
        {
            Text = "   "
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WithWhitespaceOnlyQuery_ReturnsBadRequest()
    {
        var searchRequest = new SearchRequest
        {
            Query = "   ",
            TopK = 5
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_InEmptyIndex_ReturnsEmptyResults()
    {
        var searchRequest = new SearchRequest
        {
            Query = "This query should return no results from empty index",
            TopK = 5
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>(cancellationToken: TestContext.Current.CancellationToken);
        results.Should().NotBeNull();
    }
}
