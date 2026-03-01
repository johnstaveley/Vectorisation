using System.Text;
using System.Text.Json;
using EmbeddingService.Models;

namespace EmbeddingService.Services;

public class DeepSeekService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeepSeekService> _logger;
    private readonly string _modelName;
    private readonly string _baseUrl;

    public DeepSeekService(IConfiguration configuration, ILogger<DeepSeekService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _baseUrl = configuration.GetConnectionString("ollama") ?? configuration["Ollama:Url"] ?? "http://localhost:50494";
        _baseUrl = _baseUrl.Replace("Endpoint=", "");
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        _logger = logger;
        _modelName = configuration["Ollama:ChatModel"] ?? "deepseek-r1:1.5b";
    }

    public async Task<string> GenerateResponseAsync(string prompt, DeepSeekOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating response for prompt: {PromptLength} characters", prompt.Length);

            var request = new
            {
                model = _modelName,
                prompt = prompt,
                stream = false,
                options = options ?? new DeepSeekOptions()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson);

            if (result == null || string.IsNullOrEmpty(result.Response))
            {
                throw new InvalidOperationException("Failed to generate response from DeepSeek model");
            }

            _logger.LogInformation("Generated response: {ResponseLength} characters in {Duration}ms", 
                result.Response.Length, result.TotalDuration / 1000000);

            return result.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response from DeepSeek model");
            throw;
        }
    }

    public async Task<string> ChatAsync(string message, List<int>? context = null, DeepSeekOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing chat message: {MessageLength} characters", message.Length);

            var request = new
            {
                model = _modelName,
                prompt = message,
                stream = false,
                context = context,
                options = options ?? new DeepSeekOptions()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson);

            if (result == null || string.IsNullOrEmpty(result.Response))
            {
                throw new InvalidOperationException("Failed to generate chat response");
            }

            return result.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat with DeepSeek model");
            throw;
        }
    }

    public async Task<bool> EnsureModelPulledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking if DeepSeek model {ModelName} is available", _modelName);

            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Could not check available models");
                return false;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var models = JsonSerializer.Deserialize<OllamaModelsResponse>(responseJson);

            if (models?.Models == null || !models.Models.Any(m => m.Name.Contains(_modelName)))
            {
                _logger.LogInformation("Model {ModelName} not found. Attempting to pull...", _modelName);
                await PullModelAsync(cancellationToken);
                return true;
            }

            _logger.LogInformation("Model {ModelName} is available", _modelName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking DeepSeek model availability");
            return false;
        }
    }

    private async Task PullModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Pulling model {ModelName}. This may take several minutes...", _modelName);

            var request = new
            {
                name = _modelName,
                stream = false
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/pull", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully pulled model {ModelName}", _modelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling model {ModelName}. Please pull manually using: ollama pull {ModelName}", 
                _modelName, _modelName);
            throw;
        }
    }

    public async Task<List<OllamaModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var models = JsonSerializer.Deserialize<OllamaModelsResponse>(responseJson);

            return models?.Models ?? new List<OllamaModelInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available models");
            return new List<OllamaModelInfo>();
        }
    }
}
