using Aspire.Components.Common.Tests;
using CommunityToolkit.Aspire.Testing;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.DotNet.XUnitExtensions;

namespace CommunityToolkit.Aspire.Hosting.Dapr.Tests;

[RequiresDocker]
public class MultiComponentEnvironmentTests(AspireIntegrationTestFixture<Projects.CommunityToolkit_Aspire_Hosting_Dapr_AppHost> fixture)
    : IClassFixture<AspireIntegrationTestFixture<Projects.CommunityToolkit_Aspire_Hosting_Dapr_AppHost>>
{
    [Fact]
    public async Task SidecarsExposeEnvironmentVariables()
    {
        string[] services = ["servicea", "serviceb", "servicec"];
        foreach (var serviceName in services)
        {
            await fixture.ResourceNotificationService
                .WaitForResourceHealthyAsync(serviceName)
                .WaitAsync(TimeSpan.FromMinutes(5));
        }

        var model = fixture.App.Services.GetRequiredService<DistributedApplicationModel>();

        foreach (var serviceName in services)
        {
            var resource = Assert.Single(model.Resources, r => r.Name == serviceName);
            var env = await ((IResourceWithEnvironment)resource)
                .GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Run);

            Assert.Contains("DAPR_HTTP_PORT", env.Keys);
            Assert.Contains("DAPR_GRPC_PORT", env.Keys);
            Assert.Contains("DAPR_HTTP_ENDPOINT", env.Keys);
            Assert.Contains("DAPR_GRPC_ENDPOINT", env.Keys);
        }
    }
}
