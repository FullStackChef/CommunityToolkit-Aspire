using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Cosmos DB
var cosmosState = builder.AddAzureCosmosDB("cosmosState")
                        .RunAsContainer(); // for local development

// Add Dapr state store with Cosmos DB reference
var daprState = builder.AddDaprStateStore("daprState")
                       .WithReference(cosmosState);

// Add a sample API service with Dapr sidecar
var api = builder.AddContainer("example-api", "mcr.microsoft.com/dotnet/samples:aspnetapp")
    .WithReference(daprState)
    .WithDaprSidecar();

builder.Build().Run();