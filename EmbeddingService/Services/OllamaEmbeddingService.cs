using System.Text;
using System.Text.Json;
using EmbeddingService.Models;

namespace EmbeddingService.Services;

public class OllamaEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaEmbeddingService> _logger;
    private readonly string _modelName;
    private readonly string _baseUrl;
    public OllamaEmbeddingService(IConfiguration configuration, ILogger<OllamaEmbeddingService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _baseUrl = configuration.GetConnectionString("ollama") ?? configuration["Ollama:Url"] ?? "http://localhost:50494";
        //_baseUrl = configuration.GetConnectionString("ollama") ?? configuration["Ollama:Url"] ?? throw new Exception("Ollama not initialised");
        _baseUrl = _baseUrl.Replace("Endpoint=", "");
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _logger = logger;
        _modelName = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
    }
    public async Task<double[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating embedding for text: {TextLength} characters", text.Length);
            var request = new
            {
                model = _modelName,
                input = text
            };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            var response = await _httpClient.PostAsync("/api/embed", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseJson);
            if (result?.Embeddings == null || result.Embeddings.Count == 0)
            {
                throw new InvalidOperationException("Failed to generate embeddings");
            }
            return result.Embeddings.First().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
    }
    public async Task EnsureModelPulledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking if model {ModelName} is available", _modelName);
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Could not check available models");
                return;
            }
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var models = JsonSerializer.Deserialize<OllamaModelsResponse>(responseJson);
            if (models?.Models == null || !models.Models.Any(m => m.Name.Contains(_modelName)))
            {
                _logger.LogWarning("Model {ModelName} not found. Please pull it manually using: ollama pull {ModelName}",
                    _modelName, _modelName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking model. It may need to be pulled manually.");
        }
    }
}

