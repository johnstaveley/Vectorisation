using EmbeddingService.Models;
using Microsoft.AspNetCore.Mvc;

namespace VectorisationWeb.Controllers;

public class EmbeddingsController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmbeddingsController> _logger;

    public EmbeddingsController(IHttpClientFactory httpClientFactory, ILogger<EmbeddingsController> logger)
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
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("ID is required");
        }

        try
        {
            var response = await _httpClient.GetAsync($"/embeddings/{id}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = $"Embedding with ID '{id}' not found";
                return RedirectToAction(nameof(Index));
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                TempData["Error"] = $"Error retrieving embedding: {error}";
                return RedirectToAction(nameof(Index));
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingDocument>(cancellationToken);
            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving embedding with id {Id}", id);
            TempData["Error"] = "An error occurred while retrieving the embedding";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(EmbeddingRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index");
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/embeddings", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                TempData["Error"] = $"Error creating embedding: {error}";
                return RedirectToAction(nameof(Index));
            }

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
            TempData["Success"] = $"Embedding created successfully with ID: {result?.Id}";
            return RedirectToAction(nameof(Details), new { id = result?.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating embedding");
            TempData["Error"] = "An error occurred while creating the embedding";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["Error"] = "ID is required";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var response = await _httpClient.DeleteAsync($"/embeddings/{id}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = $"Embedding with ID '{id}' not found";
                return RedirectToAction(nameof(Index));
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                TempData["Error"] = $"Error deleting embedding: {error}";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = $"Embedding with ID '{id}' deleted successfully";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting embedding with id {Id}", id);
            TempData["Error"] = "An error occurred while deleting the embedding";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAll(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.DeleteAsync("/embeddings", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                TempData["Error"] = $"Error deleting all embeddings: {error}";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "All embeddings deleted successfully";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all embeddings");
            TempData["Error"] = "An error occurred while deleting all embeddings";
            return RedirectToAction(nameof(Index));
        }
    }
}
