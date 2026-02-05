# Kepware.Api

[![Build Status](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions)
[![NuGet](https://img.shields.io/nuget/v/Kepware.Api.svg)](https://www.nuget.org/packages/Kepware.Api/)

## Overview
The `Kepware.Api` library provides a robust client implementation to interact with the Kepware Configuration API. It supports managing channels, devices, tags, and other configurations programmatically while ensuring secure and efficient communication.

This package is designed to work with all versions of Kepware that support the Configuration API including Kepware Server (KS), and Kepware Edge (KE). For reference, Kepware Server in this documentation will also imply Thingworx Kepware Server and KEPServerEX versions prior to v7.0 when v6.x is referenced.

## Features

1. Connect to Kepware Configuration APIs securely with HTTPS and optional certificate validation.
2. Perform CRUD operations for the following Kepware configuration objects:

| Features     | KS           | KE           |
| :----------: | :----------: | :----------: |
| **Project Properties** | Y | Y |
| **Connectivity** <br /> *(Channel, Devices, Tags, Tag Groups)* | Y | Y |
| **Administration** <br /> *(User Groups, Users, UA Endpoints, Local License Server)* | Y[^1] | Y |
| **Product Info and Health Status** | Y[^4] | Y |
| **Export Project** | Y[^2] | Y |
| **Import Project (via JsonProjectLoad Service)** | N[^2] | N |
| **Import Project (via CompareAndApply)[^3]** | Y | Y |

[^1]: UA Endpoints and Local License Server supported for Kepware Edge only
[^2]: JsonProjectLoad was added to Kepware Server v6.17 and later builds, the SDK detects the server version and uses the appropriate service or loads the project by multiple requests if using KepwareApiClient.LoadProject.
[^3]: CompareAndApply is handled by the SDK, it compares the source project with the server project and applies the changes. The JsonProjectLoad service is a direct call to the server to load a project.
[^4]: Added to Kepware Server v6.13 and later builds

3. Configuration API *Services* implemented:

| Services     | KS           | KE           |
| :----------: | :----------: | :----------: |
| **TagGeneration** <br /> *(for supported drivers)* | Y | Y |
| **ReinitializeRuntime** | Y* | Y |
| **ProjectLoad and ProjectSave** | N | N |
| **JsonProjectLoad\*\*** <br /> *(used for import project feature)* | Y | Y |

4.  Synchronize configurations between your application and Kepware server.
5.  Supports advanced operations like project comparison, entity synchronization, and driver property queries.
6.  Built-in support for Dependency Injection to simplify integration.

## Installation

Kepware.Api NuGet package is available from NuGet repository.

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
       apiPassword: "StrongAdminPassword2025!",
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
  var drivers = await api.GetSupportedDriversAsync();
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
This SDK is provided "as is" under the MIT License. See the [LICENSE](../LICENSE.txt) file for details.

## Support
For any issues, please open an Issue within the repository. For questions or feature requests, please open a Discussion thread within the repository. 

See [Repository Guidelines](../docs/repo-guidelines.md) for more information.

