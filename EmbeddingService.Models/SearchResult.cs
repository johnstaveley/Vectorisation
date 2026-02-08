namespace EmbeddingService.Models;

public class SearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Score { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
