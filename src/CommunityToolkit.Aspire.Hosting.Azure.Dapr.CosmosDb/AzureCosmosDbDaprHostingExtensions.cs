using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using CommunityToolkit.Aspire.Hosting.Azure.Dapr;
using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring Dapr components with Azure Cosmos DB.
/// </summary>
public static class AzureCosmosDbDaprHostingExtensions
{
    private const string secretStoreComponentKey = "secretStoreComponent";
    private const string cosmosKeyVaultNameKey = "cosmosKeyVaultName";
    private const string cosmosUrlKey = "cosmosUrl";
    private const string daprConnectionStringKey = "daprConnectionString";

    /// <summary>
    /// Configures a Dapr component resource to use an Azure Cosmos DB resource.
    /// </summary>
    /// <param name="builder">The Dapr component resource builder.</param>
    /// <param name="source">The Azure Cosmos DB resource builder.</param>
    /// <returns>The updated Dapr component resource builder.</returns>
    public static IResourceBuilder<IDaprComponentResource> WithReference(this IResourceBuilder<IDaprComponentResource> builder, IResourceBuilder<AzureCosmosDBResource> source)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // For local development, we'll use the connection string from the Cosmos DB emulator
            // The CosmosDB emulator connection string is well-known
            builder.WithMetadata("url", "https://localhost:8081/");
            builder.WithMetadata("masterKey", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
            builder.WithMetadata("database", source.Resource.Name);
            
            return builder;
        }

        return builder.Resource.Type switch
        {
            "state" => builder.ConfigureCosmosDbStateComponent(source),
            _ => throw new InvalidOperationException($"Unsupported Dapr component type '{builder.Resource.Type}' for Cosmos DB. Only 'state' components are supported.")
        };
    }

    // Private methods do not require XML documentation.

    private static IResourceBuilder<IDaprComponentResource> ConfigureCosmosDbStateComponent(this IResourceBuilder<IDaprComponentResource> builder, IResourceBuilder<AzureCosmosDBResource> cosmosDbBuilder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(cosmosDbBuilder, nameof(cosmosDbBuilder));

        if (cosmosDbBuilder.Resource.UseAccessKeyAuthentication)
        {
            builder.ConfigureForAccessKeyAuthentication(cosmosDbBuilder, "state.cosmosdb");
        }
        else
        {
            builder.ConfigureForManagedIdentityAuthentication(cosmosDbBuilder, "state.cosmosdb");
        }

        return builder;
    }

    private static void ConfigureForManagedIdentityAuthentication(this IResourceBuilder<IDaprComponentResource> builder, IResourceBuilder<AzureCosmosDBResource> cosmosDbBuilder, string componentType)
    {
        var principalIdParam = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var cosmosUrlParam = cosmosDbBuilder.GetOutput(daprConnectionStringKey).AsProvisioningParameter(infrastructure, cosmosUrlKey);

            if (infrastructure.GetProvisionableResources().OfType<ContainerAppManagedEnvironment>().FirstOrDefault() is ContainerAppManagedEnvironment managedEnvironment)
            {
                var daprComponent = AzureDaprHostingExtensions.CreateDaprComponent(
                    builder.Resource.Name,
                    BicepFunction.Interpolate($"{builder.Resource.Name}"),
                    componentType,
                    "v1");

                daprComponent.Parent = managedEnvironment;

                var metadata = new List<ContainerAppDaprMetadata>
                {
                    new() { Name = "url", Value = cosmosUrlParam },
                    new() { Name = "database", Value = cosmosDbBuilder.Resource.Name },
                    new() { Name = "collection", Value = "statestore" }, // Default collection name for state store
                    new() { Name = "actorStateStore", Value = "true" }
                };

                daprComponent.Metadata = [.. metadata];

                // Add scopes if any exist
                builder.AddScopes(daprComponent);

                infrastructure.Add(daprComponent);

                infrastructure.TryAdd(cosmosUrlParam);
            }
        };

        builder.WithAnnotation(new AzureDaprComponentPublishingAnnotation(configureInfrastructure));

        // Configure the Cosmos DB resource to output the connection URL
        cosmosDbBuilder.ConfigureInfrastructure(infrastructure =>
        {
            var cosmosAccount = infrastructure.GetProvisionableResources().OfType<CosmosDBAccount>().SingleOrDefault();
            var outputExists = infrastructure.GetProvisionableResources().OfType<ProvisioningOutput>().Any(o => o.BicepIdentifier == daprConnectionStringKey);

            if (cosmosAccount is not null && !outputExists)
            {
                infrastructure.Add(new ProvisioningOutput(daprConnectionStringKey, typeof(string))
                {
                    Value = BicepFunction.Interpolate($"{cosmosAccount.DocumentEndpoint}")
                });
            }
        });
    }

    private static void ConfigureForAccessKeyAuthentication(this IResourceBuilder<IDaprComponentResource> builder, IResourceBuilder<AzureCosmosDBResource> cosmosDbBuilder, string componentType)
    {
        var kvNameParam = new ProvisioningParameter(cosmosKeyVaultNameKey, typeof(string));
        var secretStoreComponent = new ProvisioningParameter(secretStoreComponentKey, typeof(string));

        // Configure Key Vault secret store component - this adds the annotation to the same resource
        builder.ConfigureKeyVaultSecretsComponent(kvNameParam);

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var cosmosUrlParam = cosmosDbBuilder.GetOutput(daprConnectionStringKey).AsProvisioningParameter(infrastructure, cosmosUrlKey);

            if (infrastructure.GetProvisionableResources().OfType<ContainerAppManagedEnvironment>().FirstOrDefault() is ContainerAppManagedEnvironment managedEnvironment)
            {
                var daprComponent = AzureDaprHostingExtensions.CreateDaprComponent(
                    builder.Resource.Name,
                    BicepFunction.Interpolate($"{builder.Resource.Name}"),
                    componentType,
                    "v1");

                daprComponent.Parent = managedEnvironment;

                var metadata = new List<ContainerAppDaprMetadata>
                {
                    new() { Name = "url", Value = cosmosUrlParam },
                    new() { Name = "database", Value = cosmosDbBuilder.Resource.Name },
                    new() { Name = "collection", Value = "statestore" }, // Default collection name for state store
                    new() { Name = "masterKey", SecretRef = "cosmos-master-key" },
                    new() { Name = "actorStateStore", Value = "true" }
                };

                daprComponent.Metadata = [.. metadata];
                daprComponent.SecretStoreComponent = secretStoreComponent;

                // Add scopes if any exist
                builder.AddScopes(daprComponent);

                infrastructure.Add(daprComponent);

                infrastructure.TryAdd(cosmosUrlParam);
                infrastructure.TryAdd(secretStoreComponent);
            }
        };

        builder.WithAnnotation(new AzureDaprComponentPublishingAnnotation(configureInfrastructure));

        // Configure the Cosmos DB resource to output the connection URL and set up Key Vault secret
        cosmosDbBuilder.ConfigureInfrastructure(infrastructure =>
        {
            var cosmosAccount = infrastructure.GetProvisionableResources().OfType<CosmosDBAccount>().SingleOrDefault();
            if (cosmosAccount is not null)
            {
                var keyVault = infrastructure.GetProvisionableResources().OfType<KeyVaultService>().SingleOrDefault();
                if (keyVault is null)
                {
                    keyVault = KeyVaultService.FromExisting("keyVault");
                    infrastructure.Add(keyVault);
                }

                var secret = new KeyVaultSecret("cosmosMasterKey")
                {
                    Parent = keyVault,
                    Name = "cosmos-master-key",
                    Properties = new SecretProperties
                    {
                        Value = cosmosAccount.GetKeys().PrimaryMasterKey
                    }
                };

                infrastructure.Add(secret);

                infrastructure.Add(new ProvisioningOutput(cosmosKeyVaultNameKey, typeof(string))
                {
                    Value = keyVault.Name
                });

                infrastructure.Add(new ProvisioningOutput(daprConnectionStringKey, typeof(string))
                {
                    Value = BicepFunction.Interpolate($"{cosmosAccount.DocumentEndpoint}")
                });
            }
        });
    }

    private static void TryAdd(this AzureResourceInfrastructure infrastructure, ProvisioningParameter provisioningParameter)
    {
        if (!infrastructure.GetProvisionableResources().OfType<ProvisioningParameter>().Any(p => p.BicepIdentifier == provisioningParameter.BicepIdentifier))
        {
            infrastructure.Add(provisioningParameter);
        }
    }
}