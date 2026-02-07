# Embedding Service API Examples

## Setup

1. Make sure Ollama is running and has the `nomic-embed-text` model:
   ```bash
   ollama pull nomic-embed-text
   ```

2. Start the Aspire AppHost (this will start Ollama, Elasticsearch, and the EmbeddingService)

## API Examples

### 1. Create an Embedding

```bash
curl -X POST http://localhost:5000/embeddings \
  -H "Content-Type: application/json" \
  -d '{
    "text": "Artificial intelligence is transforming the world",
    "metadata": {
      "source": "article",
      "category": "technology"
    }
  }'
```

Response:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "embedding": [0.123, -0.456, 0.789, ...],
  "text": "Artificial intelligence is transforming the world"
}
```

### 2. Search for Similar Text

```bash
curl -X POST http://localhost:5000/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "machine learning and AI",
    "topK": 5
  }'
```

Response:
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "text": "Artificial intelligence is transforming the world",
    "score": 0.95,
    "metadata": {
      "source": "article",
      "category": "technology"
    }
  }
]
```

### 3. Get Embedding by ID

```bash
curl http://localhost:5000/embeddings/550e8400-e29b-41d4-a716-446655440000
```

### 4. Delete an Embedding

```bash
curl -X DELETE http://localhost:5000/embeddings/550e8400-e29b-41d4-a716-446655440000
```

## Using with PowerShell

```powershell
# Create embedding
$body = @{
    text = "Artificial intelligence is transforming the world"
    metadata = @{
        source = "article"
        category = "technology"
    }
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "http://localhost:5000/embeddings" `
    -ContentType "application/json" -Body $body

# Search
$searchBody = @{
    query = "machine learning and AI"
    topK = 5
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "http://localhost:5000/search" `
    -ContentType "application/json" -Body $searchBody
```

## Using with C#

```csharp
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

// Create embedding
var request = new { text = "Artificial intelligence is transforming the world" };
var response = await client.PostAsJsonAsync("/embeddings", request);
var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();

// Search
var searchRequest = new { query = "machine learning and AI", topK = 5 };
var searchResponse = await client.PostAsJsonAsync("/search", searchRequest);
var searchResults = await searchResponse.Content.ReadFromJsonAsync<List<SearchResult>>();
```

## Configuration

Update `appsettings.json` to customize:

```json
{
  "Ollama": {
    "EmbeddingModel": "nomic-embed-text"
  },
  "Elasticsearch": {
    "IndexName": "embeddings"
  }
}
```

## Architecture

1. **Ollama Integration**: Uses Ollama's API to generate 768-dimensional embeddings using the nomic-embed-text model
2. **Elasticsearch Storage**: Stores embeddings with vector search capabilities using cosine similarity
3. **Semantic Search**: Converts search queries to embeddings and finds similar documents using k-NN search

## Use Cases

- Semantic document search
- Text similarity detection
- Content recommendation
- Duplicate detection
- Question answering systems
- Document clustering
