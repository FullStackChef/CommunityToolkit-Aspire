@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param userPrincipalId string = ''

param tags object = { }

param redisHost string

param redisstate_outputs_name string

resource cae_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('cae_mi-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: tags
}

resource cae_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' = {
  name: take('caeacr${uniqueString(resourceGroup().id)}', 50)
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
}

resource cae_acr_cae_mi_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(cae_acr.id, cae_mi.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  properties: {
    principalId: cae_mi.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
  scope: cae_acr
}

resource cae_law 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: take('caelaw-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

resource cae 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: take('cae${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: cae_law.properties.customerId
        sharedKey: cae_law.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
  tags: tags
}

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
  name: 'aspire-dashboard'
  properties: {
    componentType: 'AspireDashboard'
  }
  parent: cae
}

resource daprManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('daprManagedIdentity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

resource redisState 'Microsoft.Cache/redis@2024-11-01' existing = {
  name: redisstate_outputs_name
}

resource redisState_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-11-01' = {
  name: guid(redisState.id, daprManagedIdentity.properties.principalId, 'Data Contributor')
  properties: {
    accessPolicyName: 'Data Contributor'
    objectId: daprManagedIdentity.properties.principalId
    objectIdAlias: daprManagedIdentity.name
  }
  parent: redisState
}

resource statestore 'Microsoft.App/managedEnvironments/daprComponents@2025-01-01' = {
  name: 'statestore'
  properties: {
    componentType: 'state.redis'
    metadata: [
      {
        name: 'redisHost'
        value: redisHost
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
      {
        name: 'useEntraID'
        value: 'true'
      }
      {
        name: 'azureClientId'
        value: daprManagedIdentity.properties.principalId
      }
      {
        name: 'actorStateStore'
        value: 'true'
      }
    ]
    scopes: [
      'servicea'
      'servicec'
    ]
    version: 'v1'
  }
  parent: cae
}

resource pubsub 'Microsoft.App/managedEnvironments/daprComponents@2025-01-01' = {
  name: 'pubsub'
  properties: {
    componentType: 'pubsub.redis'
    metadata: [
      {
        name: 'redisHost'
        value: redisHost
      }
      {
        name: 'enableTLS'
        value: 'true'
      }
      {
        name: 'useEntraID'
        value: 'true'
      }
      {
        name: 'azureClientId'
        value: daprManagedIdentity.properties.principalId
      }
    ]
    scopes: [
      'servicea'
      'serviceb'
    ]
    version: 'v1'
  }
  parent: cae
}

output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = cae_law.name

output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = cae_law.id

output AZURE_CONTAINER_REGISTRY_NAME string = cae_acr.name

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = cae_acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = cae_mi.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = cae.name

output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = cae.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = cae.properties.defaultDomain