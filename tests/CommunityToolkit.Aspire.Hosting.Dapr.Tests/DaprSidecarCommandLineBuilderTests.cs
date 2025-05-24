using System.Linq;
using System.Collections.Immutable;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Xunit;

namespace CommunityToolkit.Aspire.Hosting.Dapr.Tests;

public class DaprSidecarCommandLineBuilderTests
{
    [Fact]
    public void Create_Builds_Expected_Arguments()
    {
        var options = new DaprSidecarOptions
        {
            AppPort = 5001,
            EnableApiLogging = true,
            Command = new[] { "myapp" }.ToImmutableList()
        };

        var commandLine = DaprSidecarCommandLineBuilder.Create(
            "dapr",
            options,
            new[] { "./resources" },
            "myapp",
            p => p);

        var args = string.Join(" ", commandLine.Arguments);

        Assert.Contains("--app-port", args);
        Assert.Contains("5001", args);
        Assert.Contains("--enable-api-logging", args);
        Assert.Contains("--resources-path", args);
    }
}
