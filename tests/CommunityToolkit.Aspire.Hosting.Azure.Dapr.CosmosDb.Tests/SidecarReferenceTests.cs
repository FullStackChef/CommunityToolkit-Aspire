using Aspire.Hosting;
using Aspire.Hosting.Utils;
using CommunityToolkit.Aspire.Hosting.Dapr;

namespace CommunityToolkit.Aspire.Hosting.Azure.Dapr.CosmosDb.Tests;

public class SidecarReferenceTests
{
    [Fact]
    public void WithDaprSidecarLambda_UsesPreferredPattern_ForReferencing_DaprCosmosDbComponents()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmosDb = builder.AddAzureCosmosDB("cosmosdb").RunAsContainer();
        var stateStore1 = builder.AddDaprStateStore("statestore1").WithReference(cosmosDb);
        var stateStore2 = builder.AddDaprStateStore("statestore2");
        
        // Act - Use the preferred pattern of attaching Dapr components to the sidecar
        builder.AddContainer("myapp", "image")
            .WithDaprSidecar(sidecar =>
            {
                sidecar.WithReference(stateStore1);
                sidecar.WithReference(stateStore2);
            });

        using var app = builder.Build();

        // Assert
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var sidecarResource = Assert.Single(appModel.Resources.OfType<IDaprSidecarResource>());
        
        // Check for component reference annotations
        var referenceAnnotations = sidecarResource.Annotations
            .OfType<DaprComponentReferenceAnnotation>()
            .ToList();
        
        // Should have 2 references: statestore1 and statestore2
        Assert.Equal(2, referenceAnnotations.Count);
        Assert.Contains(referenceAnnotations, a => a.Component.Name == "statestore1");
        Assert.Contains(referenceAnnotations, a => a.Component.Name == "statestore2");
    }
    
    [Fact]
    public void CosmosDbStateStoreCanBeReferencedDirectlyBySidecar()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmosDb = builder.AddAzureCosmosDB("cosmosdb").RunAsContainer();
        var stateStore = builder.AddDaprStateStore("statestore").WithReference(cosmosDb);
        var pubSub = builder.AddDaprPubSub("pubsub");
        
        // Act - Reference multiple components directly from the sidecar
        builder.AddContainer("myapp", "image")
            .WithDaprSidecar(sidecar => {
                sidecar.WithReference(stateStore);
                sidecar.WithReference(pubSub);
            });

        using var app = builder.Build();

        // Assert
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var sidecarResource = Assert.Single(appModel.Resources.OfType<IDaprSidecarResource>());
        
        // Check for component reference annotations
        var referenceAnnotations = sidecarResource.Annotations
            .OfType<DaprComponentReferenceAnnotation>()
            .ToList();
        
        Assert.Equal(2, referenceAnnotations.Count);
        Assert.Contains(referenceAnnotations, a => a.Component.Name == "statestore");
        Assert.Contains(referenceAnnotations, a => a.Component.Name == "pubsub");
    }
}