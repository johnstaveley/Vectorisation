using System.Text.Json.Serialization;

namespace EmbeddingService.Models;

public class DeepSeekRequest
{
    public string Prompt { get; set; } = string.Empty;
    public DeepSeekOptions? Options { get; set; }
}

public class DeepSeekOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 0.9;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2000;
}
