namespace EmbeddingService.Models;

public class EmbeddingRequest
{
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
}

public class EmbeddingResponse
{
    public string Id { get; set; } = string.Empty;
    public double[] Embedding { get; set; } = Array.Empty<double>();
    public string Text { get; set; } = string.Empty;
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
}

public class SearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Score { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
