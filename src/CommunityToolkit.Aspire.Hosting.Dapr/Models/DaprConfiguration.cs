using System;
using Corvus.Json;

namespace CommunityToolkit.Aspire.Hosting.Dapr.Models;

/// <summary>
/// Represents a Dapr Configuration configuration.
/// </summary>
[JsonSchemaTypeGenerator("./Specs/configuration-schema.json")]
public readonly partial struct DaprConfiguration
{

}
