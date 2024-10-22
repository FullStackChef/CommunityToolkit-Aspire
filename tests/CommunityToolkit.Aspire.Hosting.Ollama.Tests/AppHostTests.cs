using CommunityToolkit.Aspire.Testing;
using Aspire.Components.Common.Tests;
using FluentAssertions;

namespace CommunityToolkit.Aspire.Hosting.Ollama.Tests;

[RequiresDocker]
public class AppHostTests(AspireIntegrationTestFixture<Projects.CommunityToolkit_Aspire_Hosting_Ollama_AppHost> fixture) : IClassFixture<AspireIntegrationTestFixture<Projects.CommunityToolkit_Aspire_Hosting_Ollama_AppHost>>
{
    [Theory]
    [InlineData("ollama")]
    [InlineData("ollama-openwebui")]
    public async Task ResourceStartsAndRespondsOk(string resourceName)
    {
        await fixture.ResourceNotificationService.WaitForResourceAsync(resourceName, KnownResourceStates.Running).WaitAsync(TimeSpan.FromMinutes(5));
        var httpClient = fixture.CreateHttpClient(resourceName);

        var response = await httpClient.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}