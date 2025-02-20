# Kepware.Api.Sample

## Overview
The `Kepware.Api.Sample` project demonstrates how to use the `Kepware.Api` library to interact with the Kepware Configuration API. It includes examples for creating channels, devices, and tags, as well as testing API connections and performing synchronization tasks.

## Features
- Connect to Kepware Configuration API using the `Kepware.Api` library.
- Create and manage channels, devices, and tags programmatically.
- Example for testing API connections.
- HTTPS support with optional certificate validation.

## Prerequisites
- A running Kepware server with the Configuration API enabled.
- .NET SDK x.0 or later.
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
               apiPassword: "InrayTkeDocker2024!",
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
                apiPassword: "InrayTkeDocker2024!",
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
This sample project is provided "as is" under the MIT License. See the [LICENSE](./LICENSE.txt) file for details.

## Support
For any issues, please open an Issue within the repository. For questions or feature requests, please open a Discussion thread within the repository. 

See [Repository Guidelines](./docs/repo-guidelines.md) for more information.
