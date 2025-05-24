using System;
using System.Collections.Generic;
using static CommunityToolkit.Aspire.Hosting.Dapr.CommandLineArgs;

namespace CommunityToolkit.Aspire.Hosting.Dapr;

internal static class DaprSidecarCommandLineBuilder
{
    public static CommandLine Create(
        string fileName,
        DaprSidecarOptions? sidecarOptions,
        IEnumerable<string> aggregateResourcesPaths,
        string appId,
        Func<string?, string?> normalizePath)
    {
        return CommandLineBuilder.Create(
            fileName,
            Command("run"),
            ModelNamedArg("--app-port", sidecarOptions?.AppPort),
            ModelNamedArg("--app-channel-address", sidecarOptions?.AppChannelAddress),
            ModelNamedArg("--app-health-check-path", sidecarOptions?.AppHealthCheckPath),
            ModelNamedArg("--app-health-probe-interval", sidecarOptions?.AppHealthProbeInterval),
            ModelNamedArg("--app-health-probe-timeout", sidecarOptions?.AppHealthProbeTimeout),
            ModelNamedArg("--app-health-threshold", sidecarOptions?.AppHealthThreshold),
            ModelNamedArg("--app-id", appId),
            ModelNamedArg("--app-max-concurrency", sidecarOptions?.AppMaxConcurrency),
            ModelNamedArg("--app-protocol", sidecarOptions?.AppProtocol),
            ModelNamedArg("--config", normalizePath(sidecarOptions?.Config)),
            ModelNamedArg("--max-body-size", sidecarOptions?.DaprMaxBodySize),
            ModelNamedArg("--read-buffer-size", sidecarOptions?.DaprReadBufferSize),
            ModelNamedArg("--dapr-internal-grpc-port", sidecarOptions?.DaprInternalGrpcPort),
            ModelNamedArg("--dapr-listen-addresses", sidecarOptions?.DaprListenAddresses),
            Flag("--enable-api-logging", sidecarOptions?.EnableApiLogging),
            Flag("--enable-app-health-check", sidecarOptions?.EnableAppHealthCheck),
            Flag("--enable-profiling", sidecarOptions?.EnableProfiling),
            ModelNamedArg("--log-level", sidecarOptions?.LogLevel),
            ModelNamedArg("--placement-host-address", sidecarOptions?.PlacementHostAddress),
            ModelNamedArg("--resources-path", aggregateResourcesPaths),
            ModelNamedArg("--run-file", normalizePath(sidecarOptions?.RunFile)),
            ModelNamedArg("--runtime-path", normalizePath(sidecarOptions?.RuntimePath)),
            ModelNamedArg("--scheduler-host-address", sidecarOptions?.SchedulerHostAddress),
            ModelNamedArg("--unix-domain-socket", sidecarOptions?.UnixDomainSocket),
            PostOptionsArgs(Args(sidecarOptions?.Command)));
    }
}
