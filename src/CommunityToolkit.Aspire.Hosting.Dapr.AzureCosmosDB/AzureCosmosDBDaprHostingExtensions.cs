using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring Dapr components with Azure CosmosDB.
/// </summary>
public static class AzureCosmosDBDaprHostingExtensions
{
    private const string cosmosDBDaprState = nameof(cosmosDBDaprState);
    /// <summary>
    /// Configures a Dapr component resource to use an Azure CosmosDB resource.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IResourceBuilder<IDaprComponentResource> WithReference(
        this IResourceBuilder<IDaprComponentResource> builder,
        IResourceBuilder<AzureCosmosDBContainerResource> source) =>
        builder.ApplicationBuilder.ExecutionContext.IsRunMode ? builder : builder.Resource.Type switch
        {
            "state" => builder.ConfigureCosmosDBStateComponent(source),
            _ => throw new InvalidOperationException($"Unsupported Dapr component type: {builder.Resource.Type}"),
        };
    /// <summary>
    /// Configures the CosmosDB state component for the Dapr component resource.
    /// </summary>
    /// <param name="builder">The Dapr component resource builder.</param>
    /// <param name="cosmosDBBuilder">The Azure CosmosDB resource builder.</param>
    /// <returns>The updated Dapr component resource builder.</returns>
    private static IResourceBuilder<IDaprComponentResource> ConfigureCosmosDBStateComponent(
        this IResourceBuilder<IDaprComponentResource> builder,
        IResourceBuilder<AzureCosmosDBContainerResource> cosmosDBBuilder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(cosmosDBBuilder, nameof(cosmosDBBuilder));

        #region relies on Aspire 9.1
        AzureCosmosDBContainerResource cosmosDBContainerResource = cosmosDBBuilder.Resource;
        AzureCosmosDBDatabaseResource cosmosDBDatabaseResource = cosmosDBContainerResource.Parent;
        AzureCosmosDBResource azureCosmosDBResource = cosmosDBDatabaseResource.Parent;
        #endregion


        var daprComponent = AzureDaprHostingExtensions.CreateDaprComponent(cosmosDBDaprState, "state.azure.cosmosdb", "v1.0");

        daprComponent.Metadata = [
            new ContainerAppDaprMetadata { Name = "url", Value = "azureCosmosDBResource.Endpoint" },
            new ContainerAppDaprMetadata { Name = "database", Value = cosmosDBDatabaseResource.Name },
            new ContainerAppDaprMetadata { Name = "collection", Value = cosmosDBContainerResource.Name }
        ];

        var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));

        var configureInfrastructure = AzureDaprHostingExtensions.GetInfrastructureConfigurationAction(daprComponent, []);
        var daprResourceBuilder = builder.AddAzureDaprResource(cosmosDBDaprState, configureInfrastructure);
        
        if (useEntraID)
        {
            daprComponent.Metadata.Add(new ContainerAppDaprMetadata
            {
                Name = "azureClientId",
                Value = principalIdParameter
            });
        }
        else
        {
            //infra.ConfigureSecretAccess(daprComponent, redisCacheResource);
            daprResourceBuilder.WithParameter("redisPasswordSecretUri", redisBuilder.GetOutput("redisPasswordSecretUri"));
        }


        // return the original builder to allow chaining
        return builder;
    }

    /// <summary>
    /// Configures secrets access for the Azure CosmosDB and sets up the necessary Dapr component secrets.
    /// </summary>
    /// <param name="cosmosDB">The Azure CosmosDB resource infrastructure.</param>
    /// <param name="daprComponent">The Dapr component for the container app managed environment.</param>
    /// <param name="cosmosDBResource">The Azure CosmosDB resource containing the keys.</param>
    private static void ConfigureSecretAccess(this AzureResourceInfrastructure cosmosDB,
                                              ContainerAppManagedEnvironmentDaprComponent daprComponent,
                                              AzureCosmosDBResource cosmosDBResource)
    {
        ArgumentNullException.ThrowIfNull(cosmosDB, nameof(cosmosDB));
        ArgumentNullException.ThrowIfNull(daprComponent, nameof(daprComponent));
        ArgumentNullException.ThrowIfNull(cosmosDBResource, nameof(cosmosDBResource));

        var cosmosDBMasterKeySecret = new ProvisioningParameter("cosmosDBMasterKeySecretUri", typeof(Uri));

        var keyVault = cosmosDB.GetProvisionableResources()
                                                 .OfType<KeyVaultService>()
                                                 .FirstOrDefault() ?? cosmosDB.ConfigureKeyVaultSecrets();

        var cosmosDBMasterKey = new KeyVaultSecret("cosmosDBMasterKey")
        {
            Parent = keyVault,
            Name = "cosmosDBMasterKey",
            Properties = new SecretProperties
            {
                Value = ""
            }
        };

        cosmosDB.Add(cosmosDBMasterKey);

        cosmosDB.Add(new ProvisioningOutput("cosmosDBMasterKeySecretUri", typeof(Uri))
        {
            Value = cosmosDBMasterKey.Properties.SecretUri
        });

        daprComponent.Metadata.Add(new ContainerAppDaprMetadata
        {
            Name = "cosmosDBMasterKey",
            SecretRef = "cosmosDBMasterKey"
        });

        daprComponent.Secrets = [
            new ContainerAppWritableSecret {
                        Name = "cosmosDBMasterKey",
                        KeyVaultUri = cosmosDBMasterKeySecret
                     }
        ];
    }
}
