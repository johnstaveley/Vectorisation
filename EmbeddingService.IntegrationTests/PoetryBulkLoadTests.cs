using CsvHelper;
using CsvHelper.Configuration;
using EmbeddingService.IntegrationTests.Models;
using EmbeddingService.Models;
using FluentAssertions;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;

namespace EmbeddingService.IntegrationTests;

[Collection("Integration Tests")]
public class PoetryBulkLoadTests
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public PoetryBulkLoadTests(ITestOutputHelper output)
    {
        _client = new DefaultHttpClientFactory().CreateClient();
        _output = output;
    }

    //[Fact(Skip="Used to load data")]
    [Fact]
    public async Task LoadSamplePoemsFromCsv_CreatesEmbeddings_Successfully()
    {
        var sampleSize = TestConfiguration.Instance.SampleSize;

        _output.WriteLine($"Loading {sampleSize} poems from embedded resource");

        var poems = LoadPoemsFromEmbeddedResource(sampleSize);
        _output.WriteLine($"Successfully parsed {poems.Count} poems from CSV");

        if (poems.Count == 0)
        {
            _output.WriteLine("No poems found in CSV file");
            return;
        }

        // Clean out existing data
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        _output.WriteLine("Cleaning out old data");
        var deleteAllResponse = await _client.DeleteAsync("/embeddings", TestContext.Current.CancellationToken);
        deleteAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        var successCount = 0;
        var failureCount = 0;
        var embeddingIds = new List<string>();
        var startTime = DateTime.UtcNow;

        foreach (var (poem, index) in poems.Select((p, i) => (p, i)))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(poem.Poem))
                {
                    _output.WriteLine($"Skipping poem {index + 1}: Empty poem text");
                    failureCount++;
                    continue;
                }

                var request = new EmbeddingRequest
                {
                    Text = poem.Poem,
                    Metadata = new Dictionary<string, string>
                    {
                        { "title", poem.Title ?? "Untitled" },
                        { "poet", poem.Poet ?? "Unknown" },
                        { "tags", poem.Tags ?? "" },
                        { "source", "Poetry Foundation" }
                    }
                };

                var response = await _client.PostAsJsonAsync("/embeddings", request, TestContext.Current.CancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: TestContext.Current.CancellationToken);
                    
                    if (embeddingResponse != null)
                    {
                        embeddingIds.Add(embeddingResponse.Id);
                        successCount++;
                        
                        if (successCount % 10 == 0)
                        {
                            _output.WriteLine($"Progress: {successCount}/{poems.Count} poems processed");
                        }
                    }
                }
                else
                {
                    _output.WriteLine($"Failed to create embedding for poem {index + 1}: {response.StatusCode}");
                    failureCount++;
                }

                if (index > 0 && index % 10 == 0)
                {
                    await Task.Delay(500, TestContext.Current.CancellationToken);
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Exception processing poem {index + 1}: {ex.Message}");
                failureCount++;
            }
        }

        var duration = DateTime.UtcNow - startTime;

        _output.WriteLine("=== Bulk Load Summary ===");
        _output.WriteLine($"Total poems processed: {poems.Count}");
        _output.WriteLine($"Successful: {successCount}");
        _output.WriteLine($"Failed: {failureCount}");
        _output.WriteLine($"Success rate: {(successCount * 100.0 / poems.Count):F2}%");
        _output.WriteLine($"Total duration: {duration.TotalSeconds:F2} seconds");
        _output.WriteLine($"Average time per poem: {duration.TotalMilliseconds / poems.Count:F2} ms");
        _output.WriteLine($"Sample embedding IDs: {string.Join(", ", embeddingIds.Take(5))}");

        successCount.Should().BeGreaterThan(0, "at least some poems should be embedded successfully");
        successCount.Should().Be(poems.Count, "all valid poems should be embedded successfully");

        if (embeddingIds.Count > 0)
        {
            _output.WriteLine("\n=== Verifying First Embedding ===");
            var firstId = embeddingIds[0];
            var getResponse = await _client.GetAsync($"/embeddings/{firstId}", TestContext.Current.CancellationToken);
            
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var document = await getResponse.Content.ReadFromJsonAsync<EmbeddingDocument>(cancellationToken: TestContext.Current.CancellationToken);
            document.Should().NotBeNull();
            document!.Metadata.Should().NotBeNull();
            document.Metadata.Should().ContainKey("title");
            document.Metadata.Should().ContainKey("poet");
            document.Metadata.Should().ContainKey("source");
            document.Metadata!["source"].Should().Be("Poetry Foundation");
            
            _output.WriteLine($"Verified embedding {firstId}:");
            _output.WriteLine($"  Title: {document.Metadata["title"]}");
            _output.WriteLine($"  Poet: {document.Metadata["poet"]}");
            _output.WriteLine($"  Embedding dimensions: {document.Embedding.Length}");
        }
    }

    private List<PoetryRecord> LoadPoemsFromEmbeddedResource(int maxRecords)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "EmbeddingService.IntegrationTests.PoetryFoundationData.csv";

        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            _output.WriteLine($"Embedded resource not found: {resourceName}");
            _output.WriteLine("Available resources:");
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                _output.WriteLine($"  - {resource}");
            }
            return new List<PoetryRecord>();
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        var records = new List<PoetryRecord>();
        csv.Read();
        csv.ReadHeader();

        while (csv.Read() && records.Count < maxRecords)
        {
            try
            {
                var record = csv.GetRecord<PoetryRecord>();
                if (record != null && !string.IsNullOrWhiteSpace(record.Poem))
                {
                    records.Add(record);
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error parsing row: {ex.Message}");
            }
        }

        return records;
    }

    private List<PoetryRecord> LoadPoemsFromCsv(string filePath, int maxRecords)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        
        var records = new List<PoetryRecord>();
        csv.Read();
        csv.ReadHeader();

        while (csv.Read() && records.Count < maxRecords)
        {
            try
            {
                var record = csv.GetRecord<PoetryRecord>();
                if (record != null && !string.IsNullOrWhiteSpace(record.Poem))
                {
                    records.Add(record);
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error parsing row: {ex.Message}");
            }
        }

        return records;
    }
}
