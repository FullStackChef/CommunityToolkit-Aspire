@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource servicea_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('servicea_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

output id string = servicea_identity.id

output clientId string = servicea_identity.properties.clientId

output principalId string = servicea_identity.properties.principalId

output principalName string = servicea_identity.name

output name string = servicea_identity.name