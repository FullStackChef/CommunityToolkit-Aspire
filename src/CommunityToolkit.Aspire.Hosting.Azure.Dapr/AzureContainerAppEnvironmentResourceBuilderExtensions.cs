using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AppContainers;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Roles;
using CommunityToolkit.Aspire.Hosting.Azure.Dapr;
using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring Azure Container App Environment resources.
/// </summary>
public static class AzureContainerAppEnvironmentResourceBuilderExtensions
{
    private const string DaprManagedIdentityKey = "daprManagedIdentity";

    /// <summary>
    /// Configures the Azure Container App Environment resource to use Dapr.
    /// This method creates a dedicated managed identity for Dapr components and configures all Dapr components to use it.
    /// </summary>
    /// <param name="builder">The Azure Container App Environment resource builder.</param>
    /// <returns>The configured Azure Container App Environment resource builder.</returns>
    public static IResourceBuilder<AzureContainerAppEnvironmentResource> WithDaprComponents(
        this IResourceBuilder<AzureContainerAppEnvironmentResource> builder)
    {
        builder.ApplicationBuilder.AddDapr(c =>
       {
           c.PublishingConfigurationAction = (IResource resource, DaprSidecarOptions? daprSidecarOptions) =>
           {
               var configureAction = (AzureResourceInfrastructure infrastructure, ContainerApp containerApp) =>
               {
                   containerApp.Configuration.Dapr = new ContainerAppDaprConfiguration
                   {
                       AppId = daprSidecarOptions?.AppId ?? resource.Name,
                       AppPort = daprSidecarOptions?.AppPort ?? 8080,
                       IsApiLoggingEnabled = daprSidecarOptions?.EnableApiLogging ?? false,
                       LogLevel = daprSidecarOptions?.LogLevel?.ToLower() switch
                       {
                           "debug" => ContainerAppDaprLogLevel.Debug,
                           "warn" => ContainerAppDaprLogLevel.Warn,
                           "error" => ContainerAppDaprLogLevel.Error,
                           _ => ContainerAppDaprLogLevel.Info
                       },
                       AppProtocol = daprSidecarOptions?.AppProtocol?.ToLower() switch
                       {
                           "grpc" => ContainerAppProtocol.Grpc,
                           _ => ContainerAppProtocol.Http,
                       },
                       IsEnabled = true
                   };
               };

               resource.Annotations.Add(new AzureContainerAppCustomizationAnnotation(configureAction));
           };
       });

        return builder.ConfigureInfrastructure(infrastructure =>
        {
            // Create the Dapr managed identity once
            var daprIdentity = new UserAssignedIdentity(DaprManagedIdentityKey);

            infrastructure.Add(daprIdentity);

            var daprComponentResources = builder.ApplicationBuilder.Resources.OfType<IDaprComponentResource>();

            // Keep track of role assignments we've already created to avoid duplicates
            var addedRoleAssignments = new HashSet<string>();

            foreach (var daprComponentResource in daprComponentResources)
            {
                if (daprComponentResource.TryGetLastAnnotation<RoleAssignmentAnnotation>(out var roleAssignmentAnnotation))
                {
                    var target = roleAssignmentAnnotation.Target.AddAsExistingResource(infrastructure);

                    foreach (var roleDefinition in roleAssignmentAnnotation.Roles)
                    {
                        // Create a unique key to check if we've already added this role assignment
                        var roleAssignmentKey = $"{roleAssignmentAnnotation.Target.GetBicepIdentifier()}_{roleDefinition.Id}";
                        
                        if (!addedRoleAssignments.Contains(roleAssignmentKey))
                        {
                            var id = new MemberExpression(new IdentifierExpression(roleAssignmentAnnotation.Target.GetBicepIdentifier()), "id");
                            var roleAssignment = new RoleAssignment($"daprManagedIdentity_{roleAssignmentAnnotation.Target.GetBicepIdentifier()}_{roleDefinition.Name}")
                            {
                                Name = BicepFunction.CreateGuid(id, daprIdentity.Id, BicepFunction.GetSubscriptionResourceId("Microsoft.Authorization/roleDefinitions", roleDefinition.Id)),
                                RoleDefinitionId = BicepFunction.GetSubscriptionResourceId("Microsoft.Authorization/roleDefinitions", roleDefinition.Id),
                                PrincipalId = daprIdentity.PrincipalId,
                                PrincipalType = RoleManagementPrincipalType.ServicePrincipal,
                                Scope = new IdentifierExpression(target.BicepIdentifier)
                            };

                            infrastructure.Add(roleAssignment);
                            addedRoleAssignments.Add(roleAssignmentKey);
                        }
                    }
                }

                daprComponentResource.TryGetLastAnnotation<AzureDaprComponentPublishingAnnotation>(out var publishingAnnotation);
                publishingAnnotation?.PublishingAction(infrastructure, daprIdentity);
            }
        });
    }
}

/// <summary>
/// Represents role definitions for Azure RBAC.
/// </summary>
/// <param name="Id">The role definition ID.</param>
/// <param name="Name">The role definition name.</param>
internal record RoleDefinition(string Id, string Name);

/// <summary>
/// Annotation that specifies role assignments for a resource.
/// </summary>
/// <param name="Target">The target Azure resource to receive role assignments.</param>
/// <param name="Roles">The collection of roles to assign.</param>
internal record RoleAssignmentAnnotation(AzureProvisioningResource Target, HashSet<RoleDefinition> Roles) : IResourceAnnotation;
