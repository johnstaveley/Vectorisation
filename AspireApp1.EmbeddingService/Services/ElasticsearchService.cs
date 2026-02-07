using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using EmbeddingService.Models;

namespace EmbeddingService.Services;

public class ElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName;
    public ElasticsearchService(ElasticsearchClient client, IConfiguration configuration,
        ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
        _indexName = configuration["Elasticsearch:IndexName"] ?? "embeddings";
    }
    public async Task InitializeIndexAsync(CancellationToken cancellationToken = default)
    {
        var existsResponse = await _client.Indices.ExistsAsync(_indexName, cancellationToken);
        if (!existsResponse.Exists)
        {
            _logger.LogInformation("Creating index {IndexName}", _indexName);
            var createResponse = await _client.Indices.CreateAsync(_indexName, c => c
                .Mappings(m => m
                    .Properties<EmbeddingDocument>(p => p
                        .Keyword(k => k.Id)
                        .Text(t => t.Text)
                        .DenseVector(d => d.Embedding, dv => dv
                            .Dims(768)
                            .Index(true)
                            .Similarity(DenseVectorSimilarity.Cosine))
                        .Date(d => d.CreatedAt)
                        .Object(o => o.Metadata)
                    )
                ), cancellationToken);
            if (!createResponse.IsValidResponse)
            {
                _logger.LogError("Failed to create index: {Error}", createResponse.ElasticsearchServerError);
                throw new InvalidOperationException($"Failed to create index: {createResponse.ElasticsearchServerError}");
            }
        }
    }
    public async Task<string> IndexEmbeddingAsync(EmbeddingDocument document,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Indexing embedding with ID {DocumentId}", document.Id);
        var response = await _client.IndexAsync(document, idx => idx.Index(_indexName), cancellationToken);
        if (!response.IsValidResponse)
        {
            _logger.LogError("Failed to index document: {Error}", response.ElasticsearchServerError);
            throw new InvalidOperationException($"Failed to index document: {response.ElasticsearchServerError}");
        }
        return document.Id;
    }
    public async Task<List<SearchResult>> SearchBySimilarityAsync(double[] queryEmbedding, int topK = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching for top {TopK} similar documents", topK);
        var floatEmbedding = queryEmbedding.Select(d => (float)d).ToArray();
        var searchResponse = await _client.SearchAsync<EmbeddingDocument>(s => s
            .Indices(_indexName)
            .Knn(k => k
                .Field(f => f.Embedding)
                .QueryVector(floatEmbedding)
                .K(topK)
                .NumCandidates(100)
            ), cancellationToken);
        if (!searchResponse.IsValidResponse)
        {
            _logger.LogError("Search failed: {Error}", searchResponse.ElasticsearchServerError);
            throw new InvalidOperationException($"Search failed: {searchResponse.ElasticsearchServerError}");
        }
        return searchResponse.Documents.Select(doc => new SearchResult
        {
            Id = doc.Id,
            Text = doc.Text,
            Score = 0,
            Metadata = doc.Metadata
        }).ToList();
    }
    public async Task<EmbeddingDocument?> GetDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync<EmbeddingDocument>(id, idx => idx.Index(_indexName), cancellationToken);
        if (!response.IsValidResponse || !response.Found)
        {
            return null;
        }
        return response.Source;
    }
    public async Task<bool> DeleteDocumentAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync<EmbeddingDocument>(id, idx => idx.Index(_indexName), cancellationToken);
        return response.IsValidResponse;
    }
}

