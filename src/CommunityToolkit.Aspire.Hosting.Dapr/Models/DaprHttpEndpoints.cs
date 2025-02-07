using System;
using Corvus.Json;

namespace CommunityToolkit.Aspire.Hosting.Dapr.Models;

/// <summary>
/// Represents a Dapr HttpEndpoint configuration.
/// </summary>
[JsonSchemaTypeGenerator("./Specs/httpendpoints-schema.json")]
public readonly partial struct DaprHttpEndpoints
{

}
