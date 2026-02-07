var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama");

var opensearch = builder.AddOpenSearch("opensearch");

builder.Build().Run();
