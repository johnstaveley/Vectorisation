var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithOpenWebUI();
//.AddModel("deepseek-r1:1.5b"); // Does not work and causes calls to the model to fail
//.AddModel("nomic-embed-text"); // Does not work and causes calls to the model to fail

var elasticsearch = builder.AddElasticsearch("elasticsearch").WithDataVolume();

var aiService = builder.AddProject("aiservice", "../AIService/AIService.csproj")
    .WithReference(ollama)
    .WithReference(elasticsearch)
    .WaitFor(ollama)
    .WaitFor(elasticsearch);

var web = builder.AddProject("webservice", "../AIWeb/AIWeb.csproj")
    .WithReference(aiService)
    .WaitFor(aiService);

builder.Build().Run();
