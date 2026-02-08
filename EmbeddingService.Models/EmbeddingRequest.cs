namespace EmbeddingService.Models;

public class EmbeddingRequest
{
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}
