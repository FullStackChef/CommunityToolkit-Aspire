using System;
using Corvus.Json;

namespace CommunityToolkit.Aspire.Hosting.Dapr.Models;

/// <summary>
/// Represents a Dapr component configuration.
/// </summary>
[JsonSchemaTypeGenerator("./Specs/component-metadata-schema.json")]
public readonly partial struct DaprComponent
{

}
