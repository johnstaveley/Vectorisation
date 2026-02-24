using EmbeddingService.Models;
using Microsoft.AspNetCore.Mvc;

namespace VectorisationWeb.Controllers;

public class SearchController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchController> _logger;

    public SearchController(IHttpClientFactory httpClientFactory, ILogger<SearchController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("embeddingservice");
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Search(SearchRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index");
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/search", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                ViewBag.Error = $"Error performing search: {error}";
                return View("Index");
            }

            var results = await response.Content.ReadFromJsonAsync<List<SearchResult>>(cancellationToken);
            ViewBag.SearchQuery = request.Query;
            return View("Results", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search with query: {Query}", request.Query);
            ViewBag.Error = "An error occurred while performing the search";
            return View("Index");
        }
    }
}
