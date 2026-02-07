# Embedding Service

This service creates text embeddings using Ollama and stores them in Elasticsearch for semantic search.

## Features

- Generate text embeddings using Ollama (nomic-embed-text model)
- Store embeddings in Elasticsearch with vector search support
- Search for similar texts using cosine similarity
- Full CRUD operations for embeddings

## API Endpoints

### Create Embedding
```
POST /embeddings
Content-Type: application/json

{
  "text": "Your text to embed",
  "metadata": {
    "source": "optional metadata"
  }
}
```

### Search Similar Texts
```
POST /search
Content-Type: application/json

{
  "query": "Search query text",
  "topK": 5
}
```

### Get Embedding by ID
```
GET /embeddings/{id}
```

### Delete Embedding
```
DELETE /embeddings/{id}
```

## Configuration

Configure the service in `appsettings.json`:

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

## Requirements

- Ollama running with the nomic-embed-text model
- Elasticsearch instance
- .NET 10

## Getting Started

1. Ensure Ollama is running and has pulled the nomic-embed-text model:
   ```
   ollama pull nomic-embed-text
   ```

2. Run the Aspire AppHost which will start all required services

3. The service will automatically:
   - Check if the Ollama model is available
   - Create the Elasticsearch index with proper vector mappings

## Example Usage

```bash
# Create an embedding
curl -X POST http://localhost:5000/embeddings \
  -H "Content-Type: application/json" \
  -d '{"text": "Machine learning is fascinating"}'

# Search for similar texts
curl -X POST http://localhost:5000/search \
  -H "Content-Type: application/json" \
  -d '{"query": "AI and deep learning", "topK": 5}'
```
