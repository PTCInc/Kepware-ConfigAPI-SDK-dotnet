# Kepware.Api

[![Build Status](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions)

## Overview
The `Kepware.Api` library provides a robust client implementation to interact with the Kepware Configuration API. It supports managing channels, devices, tags, and other configurations programmatically while ensuring secure and efficient communication.

This package is designed to work with all versions of Kepware that support the Configuration API including Thingworx Kepware Server (TKS), Thingworx Kepware Edge (TKE) and KEPServerEX (KEP). For reference, Kepware Server in this documentation will refer to both TKS and KEP versions.

## Features
1. Connect to Kepware Configuration APIs securely with HTTPS and optional certificate validation.
2. Perform CRUD operations for the following Kepware configuration objects:

| Features      | TKS/KEP       | TKE           |
| :----------:  | :----------:  | :----------:  |
| **Connectivity** <br /> *(Channel, Devices, Tags, Tag Groups)* | Y | Y |


3.  Synchronize configurations between your application and Kepware server.
4.  Supports advanced operations like project comparison, entity synchronization, and driver property queries.
5.  Built-in support for Dependency Injection to simplify integration.

## Installation

To install the package via the GitHub NuGet feed, you need to configure your project to use the GitHub Packages repository.

### Add the GitHub NuGet source

Add the following configuration to your `NuGet.config` file in the root of your project or in the global configuration directory:

```xml
<configuration>
  <packageSources>
    <add key="GitHub" value="https://nuget.pkg.github.com/YourUsername/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <GitHub>
      <add key="Username" value="YourUsername" />
      <add key="ClearTextPassword" value="YOUR_PERSONAL_ACCESS_TOKEN" />
    </GitHub>
  </packageSourceCredentials>
</configuration>
```

1. Add the `Kepware.Api` library to your project as a reference.
   ```bash
   dotnet add package Kepware.Api
   ```

2. Register the `KepwareApiClient` in your application using Dependency Injection:
   ```csharp
   services.AddKepwareApiClient(
       name: "default",
       baseUrl: "https://localhost:57512",
       apiUserName: "Administrator",
       apiPassword: "password",
       disableCertificateValidation: true
   );
   ```

## Key Methods

### Connection and Status
- **Test Connection:**
  ```csharp
  var isConnected = await api.TestConnectionAsync();
  ```
  Tests the connection to the Kepware server. Returns `true` if successful.

- **Get Product Info:**
  ```csharp
  var productInfo = await api.GetProductInfoAsync();
  ```
  Retrieves product information about the Kepware server.

### Project Management
- **Load Project:**
  ```csharp
  var project = await api.LoadProject(blnLoadFullProject:true);
  ```
  Loads the current project from the Kepware server.

- **Compare and Apply Project:**
  ```csharp
  var result = await api.CompareAndApply(sourceProject);
  ```
  Compares a source project with the Kepware server's project and applies changes.

### Entity Operations
#### Channels
- **Get or Create Channel:**
  ```csharp
  var channel = await api.GetOrCreateChannelAsync("Channel1", "Simulator");
  ```
  Retrieves an existing channel or creates a new one.

#### Devices
- **Get or Create Device:**
  ```csharp
  var device = await api.GetOrCreateDeviceAsync(channel, "Device1", "Simulator");
  ```
  Retrieves an existing device or creates a new one under the specified channel.

#### Tags
- **Synchronize Tags:**
  ```csharp
  var tags = new DeviceTagCollection(new[]
  {
      new Tag { Name = "Ramp", TagAddress = "RAMP (0, 100, 1)" },
      new Tag { Name = "Sine", TagAddress = "SINE (0, 360, 0.1)" }
  });
  await api.CompareAndApply(tags, device.Tags, device);
  ```

### Driver Properties
- **Supported Drivers:**
  ```csharp
  var drivers = await api.SupportedDriversAsync();
  ```
  Retrieves a list of supported drivers and their details.

### CRUD Operations
- **Update Item:**
  ```csharp
  await api.UpdateItemAsync(device);
  ```

- **Insert Item:**
  ```csharp
  await api.InsertItemAsync<ChannelCollection,Channel>(channel);
  ```

- **Delete Item:**
  ```csharp
  await api.DeleteItemAsync(device);
  ```

## Licensing
This SDK is provided "as is" under the MIT License. See the [LICENSE](./LICENSE.txt) file for details.

## Support
For any issues, please open an Issue within the repository. For questions or feature requests, please open a Discussion thread within the repository. 

See [Repository Guidelines](./docs/repo-guidelines.md) for more information.

