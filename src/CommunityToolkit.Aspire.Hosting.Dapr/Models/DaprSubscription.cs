using System;
using Corvus.Json;

namespace CommunityToolkit.Aspire.Hosting.Dapr.Models;

/// <summary>
/// Represents a Dapr Subscription configuration.
/// </summary>
[JsonSchemaTypeGenerator("./Specs/subscription-schema.json")]
public readonly partial struct DaprSubscription
{

}
