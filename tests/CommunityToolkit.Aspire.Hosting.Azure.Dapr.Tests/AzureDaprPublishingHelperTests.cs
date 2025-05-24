using Aspire.Hosting.Utils;
using Aspire.Hosting;
using StackExchange.Redis;
using Aspire.Hosting.Azure;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Azure.Provisioning.AppContainers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CommunityToolkit.Aspire.Hosting.Azure.Dapr.Tests;
public class AzureDaprPublishingHelperTests
{
    [Fact]
    public async Task ExecuteProviderSpecificRequirements_AddsAzureContainerAppCustomizationAnnotation_WhenPublishAsAzureContainerAppIsUsed()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var redisState = builder.AddAzureRedis("redisState").RunAsContainer();

        var daprState = builder.AddDaprStateStore("daprState");

        builder.AddContainer("name", "image")
                .PublishAsAzureContainerApp((infrastructure, container) => { })
                .WithReference(daprState)
                .WithDaprSidecar();

        using var app = builder.Build();

       await ExecuteBeforeStartHooksAsync(app, default);

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());

        Assert.Equal(2, containerResource.Annotations.OfType<AzureContainerAppCustomizationAnnotation>().Count());
    }

    [Fact]
    public async Task ExecuteProviderSpecificRequirements_ConfiguresDaprSettings_FromSidecarOptions()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var redisState = builder.AddAzureRedis("redisState").RunAsContainer();

        var daprState = builder.AddDaprStateStore("daprState");

        builder.AddContainer("name", "image")
                .PublishAsAzureContainerApp((infra, container) => { })
                .WithReference(daprState)
                .WithDaprSidecar(new DaprSidecarOptions
                {
                    AppId = "myid",
                    AppPort = 1234,
                    EnableApiLogging = true,
                    LogLevel = "warn",
                    AppProtocol = "grpc"
                });

        using var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.GetContainerResources());

        var annotation = containerResource.Annotations.OfType<AzureContainerAppCustomizationAnnotation>().Last();

        var customizationProperty = annotation.GetType().GetProperty("Customization")
                                    ?? annotation.GetType().GetProperty("Customize")
                                    ?? annotation.GetType().GetProperty("Callback")
                                    ?? annotation.GetType().GetProperty("Configure");
        Assert.NotNull(customizationProperty);

        var customization = (Action<AzureResourceInfrastructure, ContainerApp>)customizationProperty!.GetValue(annotation)!;

        var containerApp = new ContainerApp("test");
        customization(null!, containerApp);

        var daprConfig = containerApp.Configuration.Dapr!;
        Assert.Equal("myid", daprConfig.AppId);
        Assert.Equal(1234, daprConfig.AppPort);
        Assert.True(daprConfig.IsApiLoggingEnabled);
        Assert.Equal(ContainerAppDaprLogLevel.Warn, daprConfig.LogLevel);
        Assert.Equal(ContainerAppProtocol.Grpc, daprConfig.AppProtocol);
        Assert.True(daprConfig.IsEnabled);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);
}
