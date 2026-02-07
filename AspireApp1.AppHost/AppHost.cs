using Aspire.Hosting;
using CommunityToolkit.Aspire.Hosting.Ollama;

var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama");

var opensearch = builder.AddElasticsearch("elasticsearch");

builder.Build().Run();
