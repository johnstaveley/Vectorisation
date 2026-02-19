using EmbeddingService.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace EmbeddingService.IntegrationTests;

public class EmbeddingServiceTests
{
    private readonly HttpClient _client;

    public EmbeddingServiceTests()
    {
        _client = new DefaultHttpClientFactory().CreateClient();
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

        var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        embeddingResponse.Should().NotBeNull();
        embeddingResponse!.Id.Should().NotBeEmpty();
        embeddingResponse.Embedding.Should().NotBeEmpty();
        embeddingResponse.Text.Should().Be(request.Text);
    }
    [Fact]
    public async Task CreateEmbedding_WithEmptyText_ReturnsBadRequest()
    {
        var request = new EmbeddingRequest
        {
            Text = ""
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task CreateEmbedding_WithNullText_ReturnsBadRequest()
    {
        var request = new EmbeddingRequest
        {
            Text = null!
        };

        var response = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task GetEmbedding_WithValidId_ReturnsDocument()
    {
        var createRequest = new EmbeddingRequest
        {
            Text = "Test document for retrieval"
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", createRequest, cancellationToken: TestContext.Current.CancellationToken);
        var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        embeddingResponse.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/embeddings/{embeddingResponse!.Id}", cancellationToken: TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = await getResponse.Content.ReadFromJsonAsync<EmbeddingDocument>(cancellationToken: TestContext.Current.CancellationToken);
        document.Should().NotBeNull();
        document!.Text.Should().Be(createRequest.Text);
        document.Embedding.Should().NotBeEmpty();
    }
    [Fact]
    public async Task GetEmbedding_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/embeddings/nonexistent-id", cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

        await _client.PostAsJsonAsync("/embeddings", embeddingRequest1, cancellationToken: TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/embeddings", embeddingRequest2, cancellationToken: TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/embeddings", embeddingRequest3, cancellationToken: TestContext.Current.CancellationToken);

        await Task.Delay(1000, cancellationToken: TestContext.Current.CancellationToken);

        var searchRequest = new SearchRequest
        {
            Query = "What is artificial intelligence?",
            TopK = 2
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>(cancellationToken: TestContext.Current.CancellationToken);
        results.Should().NotBeNull();
        results!.Should().HaveCountLessThanOrEqualTo(2);

        if (results.Count > 0)
        {
            results[0].Id.Should().NotBeEmpty();
            results[0].Text.Should().NotBeEmpty();
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

        var response = await _client.PostAsJsonAsync("/search", searchRequest, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchSimilar_WithNullQuery_ReturnsBadRequest()
    {
        var searchRequest = new SearchRequest
        {
            Query = null!,
            TopK = 5
        };

        var response = await _client.PostAsJsonAsync("/search", searchRequest, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteEmbedding_WithValidId_ReturnsNoContent()
    {
        var createRequest = new EmbeddingRequest
        {
            Text = "Document to be deleted"
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", createRequest, cancellationToken: TestContext.Current.CancellationToken);
        var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        embeddingResponse.Should().NotBeNull();

        var deleteResponse = await _client.DeleteAsync($"/embeddings/{embeddingResponse!.Id}", cancellationToken: TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/embeddings/{embeddingResponse.Id}", cancellationToken: TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEmbedding_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/embeddings/nonexistent-id", cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

        var createResponse = await _client.PostAsJsonAsync("/embeddings", request, cancellationToken: TestContext.Current.CancellationToken);
        var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
        embeddingResponse.Should().NotBeNull();

        var getResponse = await _client.GetAsync($"/embeddings/{embeddingResponse!.Id}", cancellationToken: TestContext.Current.CancellationToken);
        var document = await getResponse.Content.ReadFromJsonAsync<EmbeddingDocument>(cancellationToken: TestContext.Current.CancellationToken);

        document.Should().NotBeNull();
        document!.Metadata.Should().NotBeNull();
        document.Metadata.Should().HaveCount(3);
        document.Metadata!["author"].Should().Be("John Doe");
        document.Metadata["category"].Should().Be("technology");
        document.Metadata["date"].Should().Be("2024-01-01");
    }
}
