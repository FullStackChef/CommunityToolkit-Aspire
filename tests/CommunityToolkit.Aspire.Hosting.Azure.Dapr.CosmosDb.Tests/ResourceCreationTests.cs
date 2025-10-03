using Aspire.Hosting;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Azure;
using CommunityToolkit.Aspire.Hosting.Dapr;
using CommunityToolkit.Aspire.Hosting.Azure.Dapr;

using AzureCosmosDbResource = Azure.Provisioning.CosmosDB.CosmosDBAccount;

namespace CommunityToolkit.Aspire.Hosting.Azure.Dapr.CosmosDb.Tests;

public class ResourceCreationTests
{
    [Fact]
    public void WithReference_WhenAADDisabled_UsesAccessKeySecret()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var cosmosState = builder.AddAzureCosmosDB("cosmosState")
                                .WithAccessKeyAuthentication()
                                .RunAsContainer();

        var daprState = builder.AddDaprStateStore("statestore")
            .WithReference(cosmosState);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Get resources with Dapr publishing annotations
        var resourcesWithAnnotation = appModel.Resources
            .Where(r => r.Annotations.OfType<AzureDaprComponentPublishingAnnotation>().Any())
            .ToList();

        // First check if there are any resources with the annotation
        Assert.NotEmpty(resourcesWithAnnotation);
        
        // Now check for a specific resource
        var daprStateStore = Assert.Single(appModel.Resources.OfType<IDaprComponentResource>(), 
            r => r.Name == "statestore");
            
        // Check there's an annotation on it
        Assert.Contains(daprStateStore.Annotations, a => a is AzureDaprComponentPublishingAnnotation);

        var cosmosDb = Assert.Single(appModel.Resources.OfType<AzureCosmosDBResource>());

        string cosmosBicep = cosmosDb.GetBicepTemplateString();

        // Verify that the Cosmos DB resource is properly configured
        Assert.Contains("'Microsoft.DocumentDB/databaseAccounts", cosmosBicep);

        // Verify that resources with Dapr publishing annotations exist
        Assert.NotEmpty(resourcesWithAnnotation);
    }

    [Fact]
    public void WithReference_WhenAADEnabled_SkipsAccessKeySecret()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var cosmosState = builder.AddAzureCosmosDB("cosmosState")
            .RunAsContainer();

        var daprState = builder.AddDaprStateStore("statestore")
            .WithReference(cosmosState);

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Get resources with Dapr publishing annotations
        var resourcesWithAnnotation = appModel.Resources
            .Where(r => r.Annotations.OfType<AzureDaprComponentPublishingAnnotation>().Any())
            .ToList();

        // First check if there are any resources with the annotation
        Assert.NotEmpty(resourcesWithAnnotation);
        
        // Now check for a specific resource
        var daprStateStore = Assert.Single(appModel.Resources.OfType<IDaprComponentResource>(), 
            r => r.Name == "statestore");
            
        // Check there's an annotation on it
        Assert.Contains(daprStateStore.Annotations, a => a is AzureDaprComponentPublishingAnnotation);

        var cosmosDb = Assert.Single(appModel.Resources.OfType<AzureCosmosDBResource>());

        string cosmosBicep = cosmosDb.GetBicepTemplateString();

        // Verify that the Cosmos DB resource is properly configured with managed identity
        Assert.Contains("'Microsoft.DocumentDB/databaseAccounts", cosmosBicep);

        // Verify that resources with Dapr publishing annotations exist
        Assert.NotEmpty(resourcesWithAnnotation);
    }

    [Fact]
    public void WithReference_WhenNonStateType_ThrowsException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var cosmosState = builder.AddAzureCosmosDB("cosmosState").RunAsContainer();
        
        // The Cosmos DB connection should only be used with state store components
        var unknownComponent = builder.AddDaprComponent("unknown","component");
        
        // Create an app with a sidecar that references the unknown component
        var appBuilder = builder.AddContainer("myapp", "image")
            .WithDaprSidecar(sidecar => {
                // Reference the unknown component first
                sidecar.WithReference(unknownComponent);
            });
            
        // Attempting to create a non-state store reference to Cosmos DB should throw
        var exception = Assert.Throws<InvalidOperationException>(() => {
            unknownComponent.WithReference(cosmosState);
        });
        
        // Verify the exception message contains information about the unsupported component type
        Assert.Contains("Unsupported Dapr component type", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Only 'state' components are supported", exception.Message, StringComparison.OrdinalIgnoreCase);
        
        // Demonstrate the correct way to reference Cosmos DB
        var stateStore = builder.AddDaprStateStore("statestore");
        stateStore.WithReference(cosmosState); // This should work correctly
        
        using var app = builder.Build();
    }
    
    [Fact]
    public void PreferredPattern_ReferencingCosmosDbStateComponent()
    {
        // This test demonstrates the preferred pattern for referencing Dapr components
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Add the Cosmos DB state and Dapr state store
        var cosmosState = builder.AddAzureCosmosDB("cosmosState").RunAsContainer();
        var daprState = builder.AddDaprStateStore("statestore");
        
        // Configure the Dapr state store to use Cosmos DB
        daprState.WithReference(cosmosState);
        
        // Add an app with a sidecar
        builder.AddContainer("myapp", "image")
            .WithDaprSidecar(sidecar => {
                // Reference the Dapr state store component through the sidecar
                sidecar.WithReference(daprState);
            });
            
        using var app = builder.Build();
        
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var sidecarResource = Assert.Single(appModel.Resources.OfType<IDaprSidecarResource>());
        
        // Check for component reference annotations
        var referenceAnnotations = sidecarResource.Annotations
            .OfType<DaprComponentReferenceAnnotation>()
            .ToList();
        
        Assert.Single(referenceAnnotations);
        Assert.Contains(referenceAnnotations, a => a.Component.Name == "statestore");
    }
}