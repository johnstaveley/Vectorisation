using System.Text;
using AIService.Models;
using OllamaSharp;

namespace AIService.Services;

public class LLMService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaApiClient _ollamaClient;
    private readonly ILogger<LLMService> _logger;
    private readonly string _modelName;

    public LLMService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<LLMService> logger)
    {
        var baseUrl = configuration.GetConnectionString("ollama") ?? configuration["Ollama:Url"] ?? "http://localhost:50494";
        baseUrl = baseUrl.Replace("Endpoint=", "");
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _ollamaClient = new OllamaApiClient(_httpClient);
        _logger = logger;
        _modelName = configuration["Ollama:ChatModel"] ?? "deepseek-r1:1.5b";
    }

    public async Task<string> GenerateResponseAsync(string prompt, LLMOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating response for prompt: {PromptLength} characters", prompt.Length);

            var responseBuilder = new StringBuilder();

            await foreach (var stream in _ollamaClient.GenerateAsync(new OllamaSharp.Models.GenerateRequest
            {
                Model = _modelName,
                Prompt = prompt,
                Options = ConvertOptions(options)
            }, cancellationToken))
            {
                responseBuilder.Append(stream?.Response);
            }

            var response = responseBuilder.ToString();

            if (string.IsNullOrEmpty(response))
            {
                throw new InvalidOperationException("Failed to generate response from chat model");
            }

            _logger.LogInformation("Generated response: {ResponseLength} characters", response.Length);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response from chat model");
            throw;
        }
    }

    public async Task<string> ChatAsync(string message, long[]? context = null, LLMOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing chat message: {MessageLength} characters", message.Length);

            var responseBuilder = new StringBuilder();

            await foreach (var stream in _ollamaClient.GenerateAsync(new OllamaSharp.Models.GenerateRequest
            {
                Model = _modelName,
                Prompt = message,
                Context = context,
                Options = ConvertOptions(options)
            }, cancellationToken))
            {
                responseBuilder.Append(stream?.Response);
            }

            var response = responseBuilder.ToString();

            if (string.IsNullOrEmpty(response))
            {
                throw new InvalidOperationException("Failed to generate chat response");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat with model");
            throw;
        }
    }

    public async Task<bool> EnsureModelPulledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking if chat model {ModelName} is available", _modelName);

            var models = await _ollamaClient.ListLocalModelsAsync(cancellationToken);

            if (models == null || !models.Any(m => m.Name.Contains(_modelName)))
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
            _logger.LogError(ex, "Error checking chat model availability");
            return false;
        }
    }

    private async Task PullModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Pulling model {ModelName}. This may take several minutes...", _modelName);

            await foreach (var status in _ollamaClient.PullModelAsync(_modelName, cancellationToken))
            {
                _logger.LogInformation("Pull progress: {Status}", status?.Status);
            }

            _logger.LogInformation("Successfully pulled model {ModelName}", _modelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling model {ModelName}. Please pull manually using: ollama pull {ModelName}", 
                _modelName, _modelName);
            throw;
        }
    }

    public async Task<IEnumerable<OllamaSharp.Models.Model>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var models = await _ollamaClient.ListLocalModelsAsync(cancellationToken);
            return models ?? Enumerable.Empty<OllamaSharp.Models.Model>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available models");
            return Enumerable.Empty<OllamaSharp.Models.Model>();
        }
    }

    private OllamaSharp.Models.RequestOptions? ConvertOptions(LLMOptions? options)
    {
        if (options == null)
            return null;

        return new OllamaSharp.Models.RequestOptions
        {
            Temperature = (float)options.Temperature,
            TopP = (float)options.TopP,
            NumPredict = options.MaxTokens
        };
    }
}
