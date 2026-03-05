using AIService.Models;
using Microsoft.AspNetCore.Mvc;

namespace VectorisationWeb.Controllers;

public class LLMController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LLMController> _logger;

    public LLMController(IHttpClientFactory httpClientFactory, ILogger<LLMController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("embeddingservice");
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Generate()
    {
        return View(new LLMRequest());
    }

    [HttpPost]
    public async Task<IActionResult> Generate(LLMRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            ModelState.AddModelError("Prompt", "Prompt is required");
            return View(request);
        }

        try
        {
            ViewBag.Prompt = request.Prompt;
            
            var response = await _httpClient.PostAsJsonAsync("/llm/generate", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                TempData["Error"] = $"Error generating response: {error}";
                return View(request);
            }

            var result = await response.Content.ReadFromJsonAsync<LLMApiResponse>(cancellationToken);
            ViewBag.Response = result?.Response;
            return View("GenerateResult", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM response");
            TempData["Error"] = "An error occurred while generating the response";
            return View(request);
        }
    }

    [HttpGet]
    public IActionResult Chat()
    {
        return View(new LLMRequest());
    }

    [HttpPost]
    public async Task<IActionResult> Chat(LLMRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            ModelState.AddModelError("Prompt", "Message is required");
            return View(request);
        }

        try
        {
            ViewBag.Message = request.Prompt;
            
            var response = await _httpClient.PostAsJsonAsync("/llm/chat", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                TempData["Error"] = $"Error in chat: {error}";
                return View(request);
            }

            var result = await response.Content.ReadFromJsonAsync<LLMApiResponse>(cancellationToken);
            ViewBag.Response = result?.Response;
            return View("ChatResult", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LLM chat");
            TempData["Error"] = "An error occurred during the chat";
            return View(request);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Models(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("/llm/models", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                TempData["Error"] = $"Error retrieving models: {error}";
                return View(new List<LLMModelInfo>());
            }

            var models = await response.Content.ReadFromJsonAsync<List<LLMModelInfo>>(cancellationToken);
            return View(models ?? new List<LLMModelInfo>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving LLM models");
            TempData["Error"] = "An error occurred while retrieving the models";
            return View(new List<LLMModelInfo>());
        }
    }
}

public class LLMApiResponse
{
    public string Response { get; set; } = string.Empty;
}

public class LLMModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Digest { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; }
}
