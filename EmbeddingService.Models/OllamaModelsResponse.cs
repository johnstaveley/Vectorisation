using System.Text.Json.Serialization;

namespace EmbeddingService.Models;

public class OllamaModelsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo> Models { get; set; } = new();
}
