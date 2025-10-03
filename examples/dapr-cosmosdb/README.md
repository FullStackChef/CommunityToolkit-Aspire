# Dapr CosmosDB Integration Example

This example demonstrates how to use Azure Cosmos DB as a Dapr state store with .NET Aspire.

## Prerequisites

- .NET 8.0 SDK
- Docker Desktop
- Azure Cosmos DB Emulator (for local development)

## Usage

The example shows the typical usage pattern for CosmosDB with Dapr in Aspire:

1. Add an Azure Cosmos DB resource
2. Configure a Dapr state store component 
3. Reference the Cosmos DB resource from the state store
4. Add applications that use the Dapr sidecar with the state store

```csharp
var cosmosState = builder.AddAzureCosmosDB("cosmosState")
                        .RunAsContainer(); 

var daprState = builder.AddDaprStateStore("daprState")
                       .WithReference(cosmosState);

var api = builder.AddContainer("example-api", "image")
    .WithReference(daprState)
    .WithDaprSidecar();
```

## Running Locally

To run this example locally:

```bash
dotnet run
```

This will start the Aspire application with:
- Cosmos DB Emulator container
- Your application with Dapr sidecar
- Dapr state store configured to use Cosmos DB

## Azure Deployment

When deploying to Azure, the integration automatically configures:
- Azure Cosmos DB account
- Dapr component with proper authentication (managed identity or access keys)
- Key Vault secrets (when using access key authentication)