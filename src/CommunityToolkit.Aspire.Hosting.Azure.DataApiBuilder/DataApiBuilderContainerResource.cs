﻿namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents Data Api Builder.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="entrypoint">An optional container entrypoint.</param>

public class DataApiBuilderContainerResource(string name, string? entrypoint = null)
    : ContainerResource(name, entrypoint), IResourceWithServiceDiscovery
{
    internal const string HttpEndpointName = "http";
    internal const string HttpsEndpointName = "https";

    internal const int HttpEndpointPort = 5000;
    internal const int HttpsEndpointPort = 5001;
}
