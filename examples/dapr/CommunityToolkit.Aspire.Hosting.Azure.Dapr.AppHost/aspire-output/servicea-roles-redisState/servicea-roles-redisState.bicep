@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param redisstate_outputs_name string

param principalId string

param principalName string

resource redisState 'Microsoft.Cache/redis@2024-11-01' existing = {
  name: redisstate_outputs_name
}

resource redisState_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-11-01' = {
  name: guid(redisState.id, principalId, 'Data Contributor')
  properties: {
    accessPolicyName: 'Data Contributor'
    objectId: principalId
    objectIdAlias: principalName
  }
  parent: redisState
}