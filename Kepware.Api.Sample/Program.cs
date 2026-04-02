using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Kepware.Api;
using Kepware.Api.Model;

namespace Kepware.Api.Sample
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Create a host builder
            using var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Add application services
                    services.AddKepwareApiClient(
                        name: "sample",
                        baseUrl: "https://localhost:57512",
                        apiUserName: "Administrator",
                        apiPassword: "ReallyStrongPassword400!",
                        disableCertificateValidation: true
                        );
                })
                .ConfigureLogging(logging =>
                {
                    // Configure logging to use the console
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);

                    logging.AddFilter("Microsoft", LogLevel.Warning);
                    logging.AddFilter("System", LogLevel.Warning);
                })
                .Build();

            // 2. Get the KepwareApiClient
            var api = host.Services.GetRequiredService<KepwareApiClient>();

            if (await api.TestConnectionAsync())
            {
                //connection is established
                var channel1 = await api.Project.Channels.GetOrCreateChannelAsync("Channel by Api", "Simulator");
                var device = await api.Project.Devices.GetOrCreateDeviceAsync(channel1, "Device by Api");

                device.Description = "Test";

                await api.Project.Devices.UpdateDeviceAsync(device);


                device.Tags = new DeviceTagCollection([
                    new Tag { Name = "RampByApi", TagAddress = "RAMP (120, 35, 100, 4)", Description ="A ramp created by the C# Api Client" },
                    new Tag { Name = "SineByApi", TagAddress = "SINE (10, -40.000000, 40.000000, 0.050000, 0)" },
                    new Tag { Name = "BooleanByApi", TagAddress = "B0001" },
                    ]);

                await api.Project.Devices.UpdateDeviceAsync(device, true);

                // --- IoT Gateway: MQTT Client Agent ---
                var mqttAgent = await api.Project.IotGateway.GetOrCreateMqttClientAgentAsync("MQTT Agent by Api");
                mqttAgent.Url = "tcp://broker.example.com:1883";
                mqttAgent.Topic = "kepware/data";
                await api.Project.IotGateway.UpdateMqttClientAgentAsync(mqttAgent);

                // Add an IoT Item referencing a tag on the device created above
                var mqttItem = await api.Project.IotGateway.GetOrCreateIotItemAsync(
                    $"{channel1.Name}.{device.Name}.BooleanByApi", mqttAgent);
                mqttItem.ScanRateMs = 500;
                await api.Project.IotGateway.UpdateIotItemAsync(mqttItem);

                // Clean up MQTT IoT Item and agent
                await api.Project.IotGateway.DeleteIotItemAsync(mqttItem);
                await api.Project.IotGateway.DeleteMqttClientAgentAsync(mqttAgent);

                // --- IoT Gateway: REST Client Agent ---
                var restClientAgent = await api.Project.IotGateway.GetOrCreateRestClientAgentAsync("REST Client by Api");
                restClientAgent.Url = "https://api.example.com/data";
                restClientAgent.HttpMethod = RestClientHttpMethod.Post;
                await api.Project.IotGateway.UpdateRestClientAgentAsync(restClientAgent);

                // Add an IoT Item to the REST Client agent
                var restClientItem = await api.Project.IotGateway.GetOrCreateIotItemAsync(
                    $"{channel1.Name}.{device.Name}.SineByApi", restClientAgent);
                await api.Project.IotGateway.DeleteIotItemAsync(restClientItem);
                await api.Project.IotGateway.DeleteRestClientAgentAsync(restClientAgent);

                // --- IoT Gateway: REST Server Agent ---
                var restServerAgent = await api.Project.IotGateway.GetOrCreateRestServerAgentAsync("REST Server by Api");
                restServerAgent.PortNumber = 39321;
                restServerAgent.EnableWriteEndpoint = true;
                await api.Project.IotGateway.UpdateRestServerAgentAsync(restServerAgent);

                // Add an IoT Item to the REST Server agent
                var restServerItem = await api.Project.IotGateway.GetOrCreateIotItemAsync(
                    $"{channel1.Name}.{device.Name}.RampByApi", restServerAgent);
                await api.Project.IotGateway.DeleteIotItemAsync(restServerItem);
                await api.Project.IotGateway.DeleteRestServerAgentAsync(restServerAgent);

                // Clean up channel and device
                await api.Project.Devices.DeleteDeviceAsync(device);
                await api.Project.Channels.DeleteChannelAsync(channel1);

                var reinitJob = await api.ApiServices.ReinitializeRuntimeAsync();

                var result = await reinitJob.AwaitCompletionAsync();

                if (result)
                {
                    Console.WriteLine("ReinitializeRuntimeAsync completed successfully.");
                }
                else
                {
                    Console.WriteLine("ReinitializeRuntimeAsync failed.");
                }

            }

            Console.WriteLine();
            Console.WriteLine("-------------------");
            Console.WriteLine("Press <Enter> to exit...");
            Console.ReadLine();
        }
    }
}
