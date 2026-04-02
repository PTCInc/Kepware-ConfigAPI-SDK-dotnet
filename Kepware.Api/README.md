# Kepware.Api

[![NuGet](https://img.shields.io/nuget/v/Kepware.Api.svg)](https://www.nuget.org/packages/Kepware.Api/)
![Dynamic XML Badge](https://img.shields.io/badge/dynamic/xml?url=https%3A%2F%2Fraw.githubusercontent.com%2FPTCInc%2FKepware-ConfigAPI-SDK-dotnet%2Frefs%2Fheads%2Fmain%2FKepware.Api%2FKepware.Api.csproj&query=%2F%2FTargetFrameworks&logo=.net&label=versions)
[![Build Status](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions/workflows/nuget-test-and-build.yml/badge.svg)](https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet/actions)
![NuGet Downloads](https://img.shields.io/nuget/dt/Kepware.Api?label=nuget%20downloads)

## Overview
The `Kepware.Api` library provides a robust client implementation to interact with the Kepware Configuration API. It supports managing channels, devices, tags, and other configurations programmatically while ensuring secure and efficient communication.

This package is designed to work with all versions of Kepware that support the Configuration API including Kepware Server (KS), Kepware Edge (KE), Thingworx Kepware Server (TKS), and KEPServerEX (KEP). For reference, when Kepware Server v6.x is referenced in this documentation, this implies Thingworx Kepware Server and KEPServerEX branded products.
## Features

1. Connect to Kepware Configuration APIs securely with HTTPS and optional certificate validation.
2. Perform CRUD operations for the following Kepware configuration objects:

| Features     | KS           | KE           |
| :----------: | :----------: | :----------: |
| **Project Properties** | Y | Y |
| **Connectivity** <br /> *(Channel, Devices, Tags, Tag Groups)* | Y | Y |
| **IoT Gateway** <br /> *(Agents, IoT Items)* | Y | Y |
| **Administration** <br /> *(User Groups, Users, UA Endpoints, Local License Server)* | Y[^1] | Y |
| **Product Info and Health Status** | Y[^4] | Y |
| **Export Project** | Y | Y |
| **Import Project (via JsonProjectLoad Service)** | N[^2] | N |
| **Import Project (via CompareAndApply)[^3]** | Y | Y |

[^2]: JsonProjectLoad was added to Kepware Server v6.17 and later builds.
[^3]: [CompareAndApply](/Kepware.Api/ClientHandler/ProjectApiHandler.cs) is handled by the SDK. It compares the source project with another server project and applies the changes.
[^4]: Added to Kepware Server v6.13 and later builds

**NOTE:** Exporting a project from a Kepware server is done using the KepwareApiClient.LoadProjectAsync method. This detects the server version and uses the appropriate method to either export/load the whole project or loads the project by multiple requests. This ensures that large projects can be exported/loaded from the Kepware instance as optimally as possible based on the current API design. See [LoadProjectAsync](/Kepware.Api/ClientHandler/ProjectApiHandler.cs) for more details.

3. Configuration API *Services* implemented:

| Services     | KS           | KE           |
| :----------: | :----------: | :----------: |
| **TagGeneration** <br /> *(for supported drivers)* | Y | Y |
| **ReinitializeRuntime** | Y* | Y |
| **ProjectLoad and ProjectSave** | N | N |
| **JsonProjectLoad\*\*** <br /> *(used for import project feature)* | N | N |

4.  Supports advanced operations like project comparison, entity synchronization, and driver property queries.
5.  Built-in support for Dependency Injection to simplify integration.

## Getting Started

### Prerequisites
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- Kepware server reachable on HTTPS port **57512** (default) or HTTP **57412**

### Step 1 — Install
```bash
dotnet add package Kepware.Api
dotnet restore
```

### Step 2 — Register the client
```csharp
// Program.cs
services.AddKepwareApiClient(
    name:        "default",
    baseUrl:     "https://localhost:57512",
    apiUserName: "Administrator",
    apiPassword: "YourPassword!",
    disableCertificateValidation: true   // false + trusted cert in production
);
```

### Step 3 — Configure credentials (never hard-code passwords)

**Environment variables (recommended)**
```bash
# Linux/macOS
export KEPWARE__PRIMARY__HOST=https://localhost:57512
export KEPWARE__PRIMARY__USERNAME=Administrator
export KEPWARE__PRIMARY__PASSWORD=YourPassword!

# Windows PowerShell
$env:KEPWARE__PRIMARY__HOST     = "https://localhost:57512"
$env:KEPWARE__PRIMARY__USERNAME = "Administrator"
$env:KEPWARE__PRIMARY__PASSWORD = "YourPassword!"
```

**appsettings.json** (add file to `.gitignore`)
```json
{
  "Kepware": {
    "Primary": {
      "Host":     "https://localhost:57512",
      "Username": "Administrator",
      "Password": "YourPassword!"
    },
    "DisableCertificateValidation": true
  }
}
```

### Step 4 — End-to-end: list channels and devices
```csharp
var api = host.Services.GetRequiredService<KepwareApiClient>();

if (!await api.TestConnectionAsync())
{
    Console.Error.WriteLine("Cannot connect — check host, port, and credentials.");
    return;
}

var info = await api.GetProductInfoAsync();
Console.WriteLine($"Connected: {info.ProductName} {info.ProductVersion}");

var project = await api.LoadProject(blnLoadFullProject: false);
foreach (var channel in project.Channels)
{
    Console.WriteLine($"Channel: {channel.Name}  Driver: {channel.DeviceDriver}");
    foreach (var device in channel.Devices)
        Console.WriteLine($"  Device: {device.Name}");
}
```

**Expected output**
```
Connected: KEPServerEX 6.14.263.0
Channel: SimulatorChannel  Driver: Simulator
  Device: SimDevice1
```

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
       disableCertificateValidation: false
   );
   ```

    or

   ```csharp
   var clientOptions = new KepwareClientOptions
   {
        HostUri = new Uri("https://localhost:57512"),
        Username = "Administrator",
        Password = "StrongAdminPassword2025!",
        Timeout = TimeSpan.FromSeconds(60),
        DisableCertifcateValidation = false,
        ProjectLoadTagLimit = 100000
   };
   
   services.AddKepwareApiClient(
       name: "default",
       options: clientOptions
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
- **Export / Load Project:**
  ```csharp
  var project = await api.LoadProjectAsync(blnLoadFullProject:true);
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

