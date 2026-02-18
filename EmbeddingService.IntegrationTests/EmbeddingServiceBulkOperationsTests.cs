using EmbeddingService.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace EmbeddingService.IntegrationTests;

public class EmbeddingServiceBulkOperationsTests
{
    private readonly HttpClient _client;

    public EmbeddingServiceBulkOperationsTests()
    {
        _client = new DefaultHttpClientFactory().CreateClient();
    }

    [Fact]
    public async Task DeleteAllEmbeddings_WithMultipleDocuments_DeletesAll()
    {
        // Arrange - Create multiple documents
        var documents = new[]
        {
            "First test document for bulk deletion",
            "Second test document for bulk deletion",
            "Third test document for bulk deletion",
            "Fourth test document for bulk deletion"
        };

        var createdIds = new List<string>();

        foreach (var text in documents)
        {
            var createRequest = new EmbeddingRequest
            {
                Text = text,
                Metadata = new Dictionary<string, string>
                {
                    { "test-type", "bulk-delete" },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                }
            };

            var createResponse = await _client.PostAsJsonAsync("/embeddings", createRequest, TestContext.Current.CancellationToken);
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(TestContext.Current.CancellationToken);
            embeddingResponse.Should().NotBeNull();
            createdIds.Add(embeddingResponse!.Id);
        }

        // Wait for Elasticsearch to index the documents
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        // Act - Delete all documents
        var deleteResponse = await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);

        // Assert - Verify deletion was successful
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResult = await deleteResponse.Content.ReadFromJsonAsync<DeleteAllResponse>(TestContext.Current.CancellationToken);
        deleteResult.Should().NotBeNull();
        deleteResult!.DeletedCount.Should().BeGreaterThan(0);

        // Wait for Elasticsearch to process deletions
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Verify documents no longer exist
        foreach (var id in createdIds)
        {
            var getResponse = await _client.GetAsync($"/embeddings/{id}", TestContext.Current.CancellationToken);
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
    [Fact]
    public async Task DeleteAllEmbeddings_WhenNoDocumentsExist_ReturnsZeroCount()
    {
        // Arrange - First delete all existing documents
        await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Act - Delete all again (should have nothing to delete)
        var deleteResponse = await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResult = await deleteResponse.Content.ReadFromJsonAsync<DeleteAllResponse>(TestContext.Current.CancellationToken);
        deleteResult.Should().NotBeNull();
        deleteResult!.DeletedCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAllEmbeddings_ThenCreateNew_WorksCorrectly()
    {
        // Arrange - Create initial documents
        var initialRequest = new EmbeddingRequest
        {
            Text = "Initial document before deletion"
        };
        await _client.PostAsJsonAsync("/embeddings", initialRequest, TestContext.Current.CancellationToken);
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Act - Delete all
        var deleteResponse = await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Create new document after deletion
        var newRequest = new EmbeddingRequest
        {
            Text = "New document after deletion",
            Metadata = new Dictionary<string, string> { { "version", "new" } }
        };

        var createResponse = await _client.PostAsJsonAsync("/embeddings", newRequest, TestContext.Current.CancellationToken);

        // Assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var embeddingResponse = await createResponse.Content.ReadFromJsonAsync<EmbeddingResponse>(TestContext.Current.CancellationToken);
        embeddingResponse.Should().NotBeNull();
        embeddingResponse!.Text.Should().Be(newRequest.Text);

        // Verify we can retrieve the new document
        var getResponse = await _client.GetAsync($"/embeddings/{embeddingResponse.Id}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = await getResponse.Content.ReadFromJsonAsync<EmbeddingDocument>(TestContext.Current.CancellationToken);
        document.Should().NotBeNull();
        document!.Text.Should().Be(newRequest.Text);
    }

    [Fact]
    public async Task DeleteAllEmbeddings_SearchAfterDeletion_ReturnsEmptyResults()
    {
        // Arrange - Create documents
        var createRequest1 = new EmbeddingRequest { Text = "Document to be deleted via bulk operation 1" };
        var createRequest2 = new EmbeddingRequest { Text = "Document to be deleted via bulk operation 2" };

        await _client.PostAsJsonAsync("/embeddings", createRequest1, TestContext.Current.CancellationToken);
        await _client.PostAsJsonAsync("/embeddings", createRequest2, TestContext.Current.CancellationToken);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        // Act - Delete all documents
        var deleteResponse = await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        // Search after deletion
        var searchRequest = new SearchRequest
        {
            Query = "Document to be deleted",
            TopK = 10
        };

        var searchResponse = await _client.PostAsJsonAsync("/search", searchRequest, TestContext.Current.CancellationToken);

        // Assert
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchResults = await searchResponse.Content.ReadFromJsonAsync<List<SearchResult>>(TestContext.Current.CancellationToken);
        searchResults.Should().NotBeNull();
        searchResults.Should().BeEmpty();
    }

    private class DeleteAllResponse
    {
        public long DeletedCount { get; set; }
    }
}
