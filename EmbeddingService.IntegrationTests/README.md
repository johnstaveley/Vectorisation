# EmbeddingService Integration Tests

This project contains comprehensive integration tests for the EmbeddingService API.

## Test Files

### 1. **EmbeddingServiceTests.cs**
Basic endpoint tests covering all CRUD operations and validation scenarios.

### 2. **EmbeddingServiceWorkflowTests.cs**
Complex workflow scenarios including:
- End-to-end workflows (create ? search ? delete)
- Search relevance verification
- Concurrent request handling
- Update workflows

### 3. **EmbeddingServiceEdgeCaseTests.cs**
Edge cases and boundary conditions:
- Very long text
- Special and Unicode characters
- Empty/null metadata
- Large TopK values
- Double deletion
- Whitespace-only inputs

## Test Coverage

The tests cover all four endpoints:

### 1. POST /embeddings
- ? Creating embeddings with valid text
- ? Creating embeddings with metadata
- ? Validation of empty text
- ? Validation of null text
- ? Validation of whitespace-only text
- ? Metadata preservation
- ? Empty and null metadata handling
- ? Very long text handling
- ? Special characters support
- ? Unicode characters support
- ? Concurrent embedding creation

### 2. GET /embeddings/{id}
- ? Retrieving existing documents
- ? Handling non-existent IDs (404)
- ? Empty ID handling

### 3. POST /search
- ? Searching with valid query
- ? Limiting results with TopK parameter
- ? Validation of empty query
- ? Validation of null query
- ? Validation of whitespace-only query
- ? TopK = 0 handling
- ? Large TopK values
- ? Search relevance ordering
- ? Empty index search

### 4. DELETE /embeddings/{id}
- ? Deleting existing documents
- ? Verifying deletion
- ? Handling non-existent IDs (404)
- ? Double deletion handling

## Running the Tests

### Prerequisites
- .NET 10 SDK
- Ollama service running with the embedding model available
- Elasticsearch instance running

### Execute Tests

```bash
dotnet test
```

### Run specific test
```bash
dotnet test --filter "FullyQualifiedName~CreateEmbedding_WithValidText_ReturnsOkWithEmbedding"
```

## Test Configuration

The tests use `WebApplicationFactory<Program>` to create an in-memory test server. The service dependencies (Ollama and Elasticsearch) need to be available for the tests to run successfully.

## Notes

- Some tests include delays (e.g., `Task.Delay(1000)`) to allow Elasticsearch to index documents before searching
- Tests are designed to be independent and can run in parallel
- Each test creates its own test data and doesn't rely on existing data
