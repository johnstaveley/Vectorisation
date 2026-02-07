using System.Text.Json.Serialization;

namespace EmbeddingService.Models;


public class OllamaEmbeddingResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("embeddings")]
    public List<List<double>> Embeddings { get; set; } = new List<List<double>>();

    [JsonPropertyName("total_duration")]
    public int TotalDuration { get; set; }

    [JsonPropertyName("load_duration")]
    public int LoadDuration { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvalCount { get; set; }
}