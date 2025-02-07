using System;
using Corvus.Json;

namespace CommunityToolkit.Aspire.Hosting.Dapr.Models;

/// <summary>
/// Represents a Dapr Resiliency configuration.
/// </summary>
[JsonSchemaTypeGenerator("./Specs/resiliency-schema.json")]
public readonly partial struct DaprResiliency
{

}
