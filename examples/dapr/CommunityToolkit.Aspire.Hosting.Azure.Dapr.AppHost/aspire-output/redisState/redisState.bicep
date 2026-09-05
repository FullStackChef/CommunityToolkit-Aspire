@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource redisState 'Microsoft.Cache/redis@2024-11-01' = {
  name: take('redisState-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 1
    }
    enableNonSslPort: false
    disableAccessKeyAuthentication: true
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'aad-enabled': 'true'
    }
  }
  tags: {
    'aspire-resource-name': 'redisState'
  }
}

output connectionString string = '${redisState.properties.hostName},ssl=true'

output name string = redisState.name

output hostName string = redisState.properties.hostName

output daprConnectionString string = '${redisState.properties.hostName}:${redisState.properties.sslPort}'