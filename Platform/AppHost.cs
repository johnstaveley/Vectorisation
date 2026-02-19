var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithOpenWebUI();
    //.AddModel("nomic-embed-text"); // Does not work and causes calls to the model to fail

var elasticsearch = builder.AddElasticsearch("elasticsearch").WithDataVolume();

var embeddingService = builder.AddProject("embeddingservice", "../EmbeddingService/EmbeddingService.csproj")
    .WithReference(ollama)
    .WithReference(elasticsearch)
    .WaitFor(ollama)
    .WaitFor(elasticsearch);

builder.Build().Run();
