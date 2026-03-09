using System.Text.Json.Serialization;

namespace AIService.Models;

public class OllamaModelsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo> Models { get; set; } = new();
}
