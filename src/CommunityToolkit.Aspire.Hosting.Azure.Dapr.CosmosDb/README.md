# CommunityToolkit.Aspire.Hosting.Azure.Dapr.CosmosDb library

An integration for running Dapr with Azure Cosmos DB state store in .NET Aspire.

## Getting started

### Prerequisites

- .NET 8.0 or later
- .NET Aspire workload

### Install the package

In your AppHost project, install the package using the following command:

```dotnetcli
dotnet add package CommunityToolkit.Aspire.Hosting.Azure.Dapr.CosmosDb
```

## Usage

To use this package, install it into your .NET Aspire AppHost project:

```bash
dotnet add package CommunityToolkit.Aspire.Hosting.Azure.Dapr.CosmosDb
```

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cosmosState = builder.AddAzureCosmosDB("cosmosState")
                        .RunAsContainer(); // for local development

var daprState = builder.AddDaprStateStore("daprState")
                       .WithReference(cosmosState); //instructs aspire to use azure cosmosdb when publishing

var api = builder.AddProject<Projects.MyApiService>("example-api")
    .WithReference(daprState)
    .WithDaprSidecar();

builder.Build().Run();

```

## Notes

The current version of the integration currently focuses on publishing and does not make any changes to how dapr components are handled in local development.

### Local Development

During local development, the integration configures the Dapr component to use the Cosmos DB emulator with the well-known connection string. Ensure you have the Azure Cosmos DB Emulator running locally or use the `.RunAsContainer()` method on your Cosmos DB resource.

### Azure Deployment

When publishing to Azure, the integration automatically configures:
- Cosmos DB account with appropriate authentication
- Dapr state store component with correct metadata
- Key Vault secrets (when using access key authentication)
- Managed identity configuration (when enabled)

### Authentication Methods

The integration supports both authentication methods:

1. **Access Key Authentication** (default): Uses master key stored in Azure Key Vault
2. **Managed Identity Authentication**: Uses Azure AD authentication (enabled by default on Azure Cosmos DB resources)

### Component Configuration

The Dapr component is configured with:
- Component type: `state.cosmosdb`
- Default collection name: `statestore`
- Actor state store enabled by default
- Database name from the Cosmos DB resource name

## Feedback & contributing

https://github.com/CommunityToolkit/Aspire