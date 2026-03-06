# Kepware.Api.Sample

## Overview
The `Kepware.Api.Sample` project demonstrates how to use the `Kepware.Api` library to interact with the Kepware Configuration API. It includes examples for creating channels, devices, and tags, as well as testing API connections and performing synchronization tasks.

## Features
- Connect to Kepware Configuration API using the `Kepware.Api` library.
- Create and manage channels, devices, and tags programmatically.
- Example for testing API connections.
- HTTPS support with optional certificate validation.

## Getting Started

### Prerequisites
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- Kepware server reachable on HTTPS **57512** or HTTP **57412**

### Step 1 — Clone and restore
```bash
git clone https://github.com/PTCInc/Kepware-ConfigAPI-SDK-dotnet.git
cd Kepware-ConfigAPI-SDK-dotnet
dotnet restore Kepware.Api.Sample/Kepware.Api.Sample.csproj
```

### Step 2 — Set credentials

Open `Kepware.Api.Sample/Program.cs` and update:
```csharp
services.AddKepwareApiClient(
    name:        "sample",
    baseUrl:     "https://localhost:57512",   // ← your Kepware host
    apiUserName: "Administrator",              // ← your username
    apiPassword: "YourPassword!",              // ← your password
    disableCertificateValidation: true
);
```

Or supply via environment variables (no file edits needed):
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

### Step 3 — Build and run
```bash
dotnet build Kepware.Api.Sample/Kepware.Api.Sample.csproj
dotnet run  --project Kepware.Api.Sample/Kepware.Api.Sample.csproj
```

### Expected output
```
Connection successful.
Channel 'Channel by Api' created.
Device 'Device by Api' created.
Tags applied: RampByApi, SineByApi, BooleanByApi

Press <Enter> to exit...
```

The sample is fully **idempotent** — running it twice will not create duplicates.

### What it does

| Step | API call | Result |
|---|---|---|
| 1 | `TestConnectionAsync()` | Verifies server is reachable and credentials are valid |
| 2 | `GetOrCreateChannelAsync("Channel by Api", "Simulator")` | Creates channel or reuses existing |
| 3 | `GetOrCreateDeviceAsync(channel, "Device by Api")` | Creates device or reuses existing |
| 4 | `CompareAndApply(desiredTags, device.Tags, device)` | Adds/updates tags without touching others |

### Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `TestConnectionAsync` → `false` | Wrong host/port | HTTPS default = **57512**, HTTP = **57412** |
| TLS error | Self-signed cert | Set `disableCertificateValidation: true` for dev |
| `401 Unauthorized` | Wrong credentials | Check username/password in Kepware User Manager |
| Channel not created | Invalid driver name | Call `api.GetSupportedDriversAsync()` to list valid drivers |

## Prerequisites
- A running Kepware server with the Configuration API enabled.
- .NET SDK 8.0 or later.
- Basic understanding of C# programming.

## Usage

### Running the Sample Application

1. **Configure the connection:**
   Update the `Program.cs` file with your Kepware API credentials and host information:
   ```csharp
   using var host = Host.CreateDefaultBuilder(args)
       .ConfigureServices((context, services) =>
       {
           services.AddKepwareApiClient(
               name: "sample",
               baseUrl: "https://localhost:57512",
               apiUserName: "Administrator",
               apiPassword: "StrongAdminPassword2025!",
               disableCertificateValidation: true
           );
       })
       .Build();
   ```

2. **Build and run the application:**
   ```bash
   dotnet build
   dotnet run
   ```

3. **Observe the output:**
   The application will:
   - Test the connection to the Kepware server.
   - Create a channel and a device if they do not already exist.
   - Add or update tags for the created device.

### Example Code
Here is a simplified version of the main application logic:

```csharp
static async Task Main(string[] args)
{
    using var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddKepwareApiClient(
                name: "sample",
                baseUrl: "https://localhost:57512",
                apiUserName: "Administrator",
                apiPassword: "StrongAdminPassword2025!",
                disableCertificateValidation: true
            );
        })
        .Build();

    var api = host.Services.GetRequiredService<KepwareApiClient>();

    if (await api.TestConnectionAsync())
    {
        var channel = await api.GetOrCreateChannelAsync("Channel by Api", "Simulator");
        var device = await api.GetOrCreateDeviceAsync(channel, "Device by Api");

        var tags = new DeviceTagCollection(new[]
        {
            new Tag { Name = "RampByApi", TagAddress = "RAMP (120, 35, 100, 4)", Description = "A ramp created by the C# Api Client" },
            new Tag { Name = "SineByApi", TagAddress = "SINE (10, -40.000000, 40.000000, 0.050000, 0)" },
            new Tag { Name = "BooleanByApi", TagAddress = "B0001" }
        });

        await api.CompareAndApply(tags, device.Tags, device);
    }

    Console.WriteLine("\nPress <Enter> to exit...");
    Console.ReadLine();
}
```

## Licensing
This sample project is provided "as is" under the MIT License. See the [LICENSE](../LICENSE.txt) file for details.

## Support
For any issues, please open an Issue within the repository. For questions or feature requests, please open a Discussion thread within the repository.

See [Repository Guidelines](../docs/repo-guidelines.md) for more information.
