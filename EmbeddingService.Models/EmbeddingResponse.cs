namespace EmbeddingService.Models;

public class EmbeddingResponse
{
    public string Id { get; set; } = string.Empty;
    public double[] Embedding { get; set; } = Array.Empty<double>();
    public string Text { get; set; } = string.Empty;
}
