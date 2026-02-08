namespace EmbeddingService.Models;

public class EmbeddingDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public double[] Embedding { get; set; } = Array.Empty<double>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string>? Metadata { get; set; }
}
