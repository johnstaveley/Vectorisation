var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithOpenWebUI();
//.AddModel("deepseek-r1:1.5b"); // Does not work and causes calls to the model to fail
//.AddModel("nomic-embed-text"); // Does not work and causes calls to the model to fail

var elasticsearch = builder.AddElasticsearch("elasticsearch").WithDataVolume();

var embeddingService = builder.AddProject("embeddingservice", "../EmbeddingService/EmbeddingService.csproj")
    .WithReference(ollama)
    .WithReference(elasticsearch)
    .WaitFor(ollama)
    .WaitFor(elasticsearch);

var web = builder.AddProject("webservice", "../VectorisationWeb/VectorisationWeb.csproj")
    .WithReference(embeddingService)
    .WaitFor(embeddingService);

builder.Build().Run();
