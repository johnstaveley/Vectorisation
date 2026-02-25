using CsvHelper.Configuration.Attributes;

namespace EmbeddingService.IntegrationTests.Models;

public class PoetryRecord
{
    [Index(0)]
    public int Index { get; set; }

    [Index(1)]
    public string Title { get; set; } = string.Empty;

    [Index(2)]
    public string Poem { get; set; } = string.Empty;

    [Index(3)]
    public string Poet { get; set; } = string.Empty;

    [Index(4)]
    public string Tags { get; set; } = string.Empty;
}
