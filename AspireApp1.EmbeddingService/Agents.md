# AI Agents Architecture

This document describes how to build AI agents using the EmbeddingService and Ollama infrastructure.

## Overview

An AI agent is an autonomous system that can:
- Process natural language queries
- Store and retrieve context using semantic search
- Make decisions based on retrieved knowledge
- Generate responses using Large Language Models (LLMs)

## Architecture Pattern

```
User Query ? Agent ? Embedding Service ? Elasticsearch (Similarity Search)
                ?
            Retrieved Context ? LLM (Ollama) ? Response
```

## Core Components

### 1. Embedding Service
- **Purpose**: Convert text to vector embeddings for semantic search
- **Model**: nomic-embed-text (768 dimensions)
- **Storage**: Elasticsearch with k-NN vector search
- **API**: RESTful endpoints for CRUD and search operations

### 2. Ollama LLM
- **Purpose**: Generate natural language responses
- **Models**: llama2, mistral, codellama, etc.
- **Integration**: HTTP API for completions

### 3. Agent Core
- **Purpose**: Orchestrate the RAG (Retrieval Augmented Generation) pipeline
- **Responsibilities**:
  - Query understanding
  - Context retrieval
  - Prompt engineering
  - Response generation

## Agent Implementation Pattern

### Basic Agent Structure

```csharp
public class BasicAgent
{
    private readonly HttpClient _embeddingClient;
    private readonly HttpClient _ollamaClient;
    private readonly ILogger<BasicAgent> _logger;
    public async Task<string> ProcessQueryAsync(string query, CancellationToken ct)
    {
        var context = await RetrieveContextAsync(query, ct);
        var prompt = BuildPrompt(query, context);
        var response = await GenerateResponseAsync(prompt, ct);
        return response;
    }
    private async Task<List<string>> RetrieveContextAsync(string query, CancellationToken ct)
    {
        var searchRequest = new { query, topK = 5 };
        var response = await _embeddingClient.PostAsJsonAsync("/search", searchRequest, ct);
        var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>(ct);
        return results.Select(r => r.Text).ToList();
    }
    private string BuildPrompt(string query, List<string> context)
    {
        var contextStr = string.Join("\n\n", context.Select((c, i) => $"[{i + 1}] {c}"));
        return $@"Based on the following context, answer the question.

Context:
{contextStr}

Question: {query}

Answer:";
    }
    private async Task<string> GenerateResponseAsync(string prompt, CancellationToken ct)
    {
        var request = new
        {
            model = "llama2",
            prompt,
            stream = false
        };
        var response = await _ollamaClient.PostAsJsonAsync("/api/generate", request, ct);
        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(ct);
        return result.Response;
    }
}
```

## Agent Types

### 1. Knowledge Base Agent
**Use Case**: Answer questions from a specific knowledge base

**Features**:
- Document ingestion pipeline
- Semantic search over documents
- Context-aware responses
- Source attribution

**Implementation**:
```csharp
public class KnowledgeBaseAgent : BasicAgent
{
    public async Task IngestDocumentAsync(string document, Dictionary<string, string> metadata)
    {
        var chunks = ChunkDocument(document);
        foreach (var chunk in chunks)
        {
            await _embeddingClient.PostAsJsonAsync("/embeddings", new
            {
                text = chunk,
                metadata = metadata
            });
        }
    }
}
```

### 2. Conversational Agent
**Use Case**: Multi-turn conversations with memory

**Features**:
- Conversation history tracking
- Context window management
- Personalization based on user preferences

**Implementation**:
```csharp
public class ConversationalAgent : BasicAgent
{
    private readonly List<Message> _conversationHistory = new();
    public async Task<string> ChatAsync(string message)
    {
        _conversationHistory.Add(new Message("user", message));
        var context = await RetrieveContextAsync(message);
        var prompt = BuildConversationalPrompt(message, context, _conversationHistory);
        var response = await GenerateResponseAsync(prompt);
        _conversationHistory.Add(new Message("assistant", response));
        return response;
    }
}
```

### 3. Task-Oriented Agent
**Use Case**: Execute specific tasks based on user intent

**Features**:
- Intent classification
- Entity extraction
- Action execution
- Result confirmation

**Example Tasks**:
- Data retrieval
- Code generation
- Report creation
- Workflow automation

### 4. Multi-Agent System
**Use Case**: Complex problems requiring specialized agents

**Pattern**:
```
Orchestrator Agent
    ??? Research Agent (information gathering)
    ??? Analysis Agent (data processing)
    ??? Writing Agent (content generation)
    ??? Review Agent (quality control)
```

## Best Practices

### 1. Chunking Strategy
```csharp
private List<string> ChunkDocument(string document, int maxChunkSize = 500, int overlap = 50)
{
    var chunks = new List<string>();
    var sentences = document.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.None);
    var currentChunk = new StringBuilder();
    foreach (var sentence in sentences)
    {
        if (currentChunk.Length + sentence.Length > maxChunkSize)
        {
            chunks.Add(currentChunk.ToString());
            currentChunk.Clear();
            currentChunk.Append(sentence);
        }
        else
        {
            currentChunk.Append(sentence + ". ");
        }
    }
    if (currentChunk.Length > 0)
        chunks.Add(currentChunk.ToString());
    return chunks;
}
```

### 2. Prompt Engineering
- **Be specific**: Clear instructions yield better results
- **Provide context**: Include relevant information in the prompt
- **Use examples**: Few-shot learning improves accuracy
- **Set constraints**: Specify format, length, and style
- **Handle errors**: Plan for unexpected responses

### 3. Context Management
```csharp
private string BuildPrompt(string query, List<string> context, int maxTokens = 2048)
{
    var systemPrompt = "You are a helpful assistant...";
    var contextStr = "";
    var tokenCount = CountTokens(systemPrompt + query);
    foreach (var ctx in context)
    {
        var ctxTokens = CountTokens(ctx);
        if (tokenCount + ctxTokens > maxTokens)
            break;
        contextStr += ctx + "\n\n";
        tokenCount += ctxTokens;
    }
    return $"{systemPrompt}\n\nContext:\n{contextStr}\n\nQuestion: {query}";
}
```

### 4. Response Quality
- **Validate outputs**: Check for hallucinations
- **Track sources**: Reference original documents
- **Implement feedback loops**: Learn from user corrections
- **Monitor performance**: Track accuracy metrics

## Configuration

### Agent Configuration Model
```csharp
public class AgentConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = "llama2";
    public int MaxContextLength { get; set; } = 2048;
    public int TopK { get; set; } = 5;
    public double Temperature { get; set; } = 0.7;
    public string SystemPrompt { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
```

### appsettings.json
```json
{
  "Agent": {
    "Model": "llama2",
    "MaxContextLength": 2048,
    "TopK": 5,
    "Temperature": 0.7,
    "ChunkSize": 500,
    "ChunkOverlap": 50
  },
  "EmbeddingService": {
    "BaseUrl": "http://localhost:5000"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "EmbeddingModel": "nomic-embed-text",
    "GenerationModel": "llama2"
  }
}
```

## Testing Agents

### Unit Testing
```csharp
[Fact]
public async Task Agent_Should_RetrieveRelevantContext()
{
    var agent = new TestAgent(_mockEmbeddingService, _mockOllama);
    var context = await agent.RetrieveContextAsync("What is machine learning?");
    Assert.NotEmpty(context);
    Assert.Contains("machine learning", context[0], StringComparison.OrdinalIgnoreCase);
}
```

### Integration Testing
```csharp
[Fact]
public async Task Agent_Should_GenerateAccurateResponse()
{
    await SeedKnowledgeBase();
    var agent = new KnowledgeBaseAgent(_embeddingClient, _ollamaClient);
    var response = await agent.ProcessQueryAsync("Explain neural networks");
    Assert.Contains("neural network", response, StringComparison.OrdinalIgnoreCase);
}
```

## Performance Optimization

### 1. Caching
- Cache embeddings for frequently accessed content
- Cache LLM responses for common queries
- Use distributed cache (Redis) for multi-instance deployments

### 2. Batch Processing
```csharp
public async Task IngestDocumentsAsync(List<string> documents)
{
    var tasks = documents.Select(doc => IngestDocumentAsync(doc));
    await Task.WhenAll(tasks);
}
```

### 3. Connection Pooling
- Use HttpClientFactory for HTTP connections
- Configure connection limits appropriately
- Implement retry policies with Polly

## Security Considerations

1. **Input Validation**: Sanitize all user inputs
2. **Prompt Injection**: Detect and prevent prompt manipulation
3. **Output Filtering**: Screen responses for sensitive information
4. **Access Control**: Implement proper authorization
5. **Rate Limiting**: Prevent abuse and manage costs

## Example Use Cases

### 1. Documentation Assistant
```csharp
var agent = new KnowledgeBaseAgent();
await agent.IngestDocumentAsync(apiDocumentation);
var answer = await agent.ProcessQueryAsync("How do I authenticate?");
```

### 2. Code Helper
```csharp
var agent = new CodeAgent();
var code = await agent.GenerateCodeAsync("Create a REST API endpoint for users");
```

### 3. Customer Support Bot
```csharp
var agent = new SupportAgent();
var response = await agent.HandleTicketAsync(customerQuery);
```

## Future Enhancements

- [ ] Multi-modal agents (text + images)
- [ ] Function calling capabilities
- [ ] External tool integration (web search, calculators)
- [ ] Automated agent training and fine-tuning
- [ ] Agent collaboration frameworks
- [ ] Real-time streaming responses
- [ ] Conversation branching and rollback

## Resources

- [Ollama Documentation](https://github.com/ollama/ollama)
- [Elasticsearch Vector Search](https://www.elastic.co/guide/en/elasticsearch/reference/current/knn-search.html)
- [Prompt Engineering Guide](https://www.promptingguide.ai/)
- [LangChain Concepts](https://python.langchain.com/docs/get_started/introduction)
- [Building LLM Applications](https://www.deeplearning.ai/short-courses/)

## Contributing

When implementing new agents:
1. Create a new class in `Agents/` folder
2. Inherit from `BasicAgent` or implement `IAgent` interface
3. Add configuration in `appsettings.json`
4. Write unit and integration tests
5. Document the agent's purpose and usage
6. Follow the single-responsibility principle
7. One class per file in the Models folder

## Coding Style Guidelines

### Method Parameters
- **Do NOT** put parameters on separate lines
- **Do** fill to the end of the line and then wrap
- **Do NOT** put blank lines between methods

**Correct:**
```csharp
public async Task<string> GenerateEmbeddingAsync(string text, Dictionary<string, string> metadata,
    CancellationToken cancellationToken = default)
{
    // method body
}
public async Task<List<SearchResult>> SearchAsync(string query, int topK, double threshold,
    CancellationToken cancellationToken = default)
{
    // method body
}
```

**Incorrect:**
```csharp
public async Task<string> GenerateEmbeddingAsync(
    string text, 
    Dictionary<string, string> metadata,
    CancellationToken cancellationToken = default)
{
    // method body
}

public async Task<List<SearchResult>> SearchAsync(
    string query,
    int topK,
    double threshold,
    CancellationToken cancellationToken = default)
{
    // method body
}
```

### Class Organization
- **One class per file** - Never put multiple classes in the same file
- **Models go in `Models/` folder** - All POCOs and DTOs belong in the Models directory
- **Services go in `Services/` folder** - Business logic and service classes
- **No nested classes** - Extract to separate files with meaningful names
- **No blank lines between method definitions** - Keep code compact

### File Structure
```
AspireApp1.EmbeddingService/
??? Models/
?   ??? EmbeddingDocument.cs
?   ??? EmbeddingRequest.cs
?   ??? EmbeddingResponse.cs
?   ??? SearchResult.cs
??? Services/
?   ??? OllamaEmbeddingService.cs
?   ??? ElasticsearchService.cs
??? Program.cs
```

### Naming Conventions
- **PascalCase** for classes, methods, properties, and public fields
- **camelCase** for local variables and parameters
- **_camelCase** for private fields
- **Descriptive names** - `GenerateEmbeddingAsync` not `GenEmbed`
- **Async suffix** - Always suffix async methods with `Async`

### Code Style
- **Use `var`** for local variables when type is obvious
- **Expression-bodied members** for simple one-liners
```csharp
public string Name => _name;
public int Calculate(int x) => x * 2;
```
- **Prefer LINQ** for collection operations
- **Use pattern matching** where appropriate
```csharp
if (result is not null)
{
    // process result
}
```
- **Nullable reference types** - Enable and use properly
```csharp
public string? OptionalValue { get; set; }
public string RequiredValue { get; set; } = string.Empty;
```

### Method Structure
- **Keep methods short** - Ideally under 20 lines
- **Single responsibility** - One method, one job
- **Early returns** - Validate and return early to reduce nesting
```csharp
public async Task<Result> ProcessAsync(Request request, CancellationToken ct)
{
    if (request is null)
        return Result.Error("Request is null");
    if (string.IsNullOrEmpty(request.Query))
        return Result.Error("Query is empty");
    var data = await FetchDataAsync(request.Query, ct);
    return Result.Success(data);
}
```

### Dependency Injection
- **Constructor injection** - Prefer constructor injection over property injection
- **Use interfaces** - Inject interfaces, not concrete types
- **Minimal constructors** - Only inject what's needed
```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly HttpClient _httpClient;
    public MyService(ILogger<MyService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }
}
```

### Error Handling
- **Use try-catch** where exceptions are expected
- **Log errors** with context
- **Throw meaningful exceptions** with descriptive messages
- **Don't swallow exceptions** - Always log or rethrow
```csharp
try
{
    return await ProcessAsync(data, ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to process data for {Id}", data.Id);
    throw;
}
```

### Async/Await
- **Always use CancellationToken** - Pass it through the call chain
- **ConfigureAwait(false)** - Use in library code, not in ASP.NET Core
- **Avoid async void** - Except for event handlers
- **Await all the way** - Don't block with `.Result` or `.Wait()`

### Configuration
- **Strongly-typed configuration** - Use the options pattern
```csharp
public class AgentOptions
{
    public string Model { get; set; } = "llama2";
    public int MaxTokens { get; set; } = 2048;
}
// In Program.cs
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agent"));
```

### Testing
- **Arrange-Act-Assert** - Structure tests clearly (but keep compact)
```csharp
[Fact]
public async Task ProcessAsync_WithValidInput_ReturnsSuccess()
{
    var service = new MyService(_mockLogger.Object, _mockHttpFactory.Object);
    var result = await service.ProcessAsync("test query", CancellationToken.None);
    Assert.True(result.IsSuccess);
}
```

### Comments
- **Avoid obvious comments** - Code should be self-documenting
- **XML documentation** - Use for public APIs
```csharp
/// <summary>
/// Generates an embedding vector for the specified text.
/// </summary>
/// <param name="text">The text to embed.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>A 768-dimensional embedding vector.</returns>
public async Task<double[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
```
- **Explain WHY, not WHAT** - Only when necessary
```csharp
// Using cosine similarity because it's normalized and works better for text embeddings
.Similarity(DenseVectorSimilarity.Cosine)
```

### Project References
- **Use project references** - Not package references for same-solution projects
- **ServiceDefaults** - Reference for common Aspire services
```xml
<ItemGroup>
  <ProjectReference Include="..\AspireApp1.ServiceDefaults\Platform.ServiceDefaults.csproj" />
</ItemGroup>
```

## Support

For issues or questions:
- Check the USAGE.md for API examples
- Review the README.md for setup instructions
- Open an issue in the GitHub repository
