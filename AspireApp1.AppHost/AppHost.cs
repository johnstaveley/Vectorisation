using Aspire.Hosting;
using CommunityToolkit.Aspire.Hosting.Ollama;

var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithOpenWebUI();

var elasticsearch = builder.AddElasticsearch("elasticsearch")
    .WithDataVolume();

var embeddingService = builder.AddProject("embeddingservice", "../AspireApp1.EmbeddingService/EmbeddingService.csproj")
    .WithReference(ollama)
    .WithReference(elasticsearch)
    .WaitFor(ollama)
    .WaitFor(elasticsearch);

builder.Build().Run();
