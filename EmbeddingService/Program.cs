using EmbeddingService.Models;
using EmbeddingService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddElasticsearchClient("elasticsearch");

builder.Services.AddHttpClient();
builder.Services.AddSingleton<OllamaEmbeddingService>();
builder.Services.AddSingleton<ElasticsearchService>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var ollamaService = app.Services.GetRequiredService<OllamaEmbeddingService>();
var elasticService = app.Services.GetRequiredService<ElasticsearchService>();

await ollamaService.EnsureModelPulledAsync();
await elasticService.InitializeIndexAsync();

app.MapPost("/embeddings", async (EmbeddingRequest request, OllamaEmbeddingService ollama, ElasticsearchService elastic, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Results.BadRequest("Text is required");
        }

        var embedding = await ollama.GenerateEmbeddingAsync(request.Text, ct);

        var document = new EmbeddingDocument
        {
            Text = request.Text,
            Embedding = embedding,
            Metadata = request.Metadata
        };

        var id = await elastic.IndexEmbeddingAsync(document, ct);

        return Results.Ok(new EmbeddingResponse
        {
            Id = id,
            Embedding = embedding,
            Text = request.Text
        });
    })
    .WithName("CreateEmbedding");

app.MapPost("/search", async (SearchRequest request, OllamaEmbeddingService ollama, ElasticsearchService elastic, CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest("Query is required");
        }

        var queryEmbedding = await ollama.GenerateEmbeddingAsync(request.Query, ct);
        var results = await elastic.SearchBySimilarityAsync(queryEmbedding, request.TopK, ct);

        return Results.Ok(results);
    })
    .WithName("SearchSimilar");

app.MapGet("/embeddings/{id}", async (string id, ElasticsearchService elastic, CancellationToken ct) =>
    {
        var document = await elastic.GetDocumentAsync(id, ct);

        if (document == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(document);
    })
    .WithName("GetEmbedding");

app.MapDelete("/embeddings/{id}", async (string id, ElasticsearchService elastic, CancellationToken ct) =>
    {
        var deleted = await elastic.DeleteDocumentAsync(id, ct);

        if (!deleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    })
    .WithName("DeleteEmbedding");

app.MapDelete("/embeddings", async (ElasticsearchService elastic, CancellationToken ct) =>
    {
        var deletedCount = await elastic.DeleteAllDocumentsAsync(ct);

        return Results.Ok(new { DeletedCount = deletedCount });
    })
    .WithName("DeleteAllEmbeddings");

app.Run();
