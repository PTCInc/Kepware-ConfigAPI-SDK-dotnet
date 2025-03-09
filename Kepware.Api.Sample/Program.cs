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
                        apiPassword: "InrayTkeDocker2024!",
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
                var channel1 = await api.GetOrCreateChannelAsync("Channel by Api", "Simulator");
                var device = await api.GetOrCreateDeviceAsync(channel1, "Device by Api");

                device.Description = "Test";

                await api.UpdateItemAsync(device);

                DeviceTagCollection tags = new DeviceTagCollection([
                     new Tag { Name = "RampByApi", TagAddress = "RAMP (120, 35, 100, 4)", Description ="A ramp created by the C# Api Client" },
                    new Tag { Name = "SineByApi", TagAddress = "SINE (10, -40.000000, 40.000000, 0.050000, 0)" },
                    new Tag { Name = "BooleanByApi", TagAddress = "B0001" },
                    ]);

                await api.CompareAndApply<DeviceTagCollection, Tag>(tags, device.Tags, device);

                await api.DeleteItemAsync(device);
                await api.DeleteItemAsync(channel1);


                var reinitJob = await api.ReinitializeRuntimeAsync();

                var result = await reinitJob.AwaitCompletionAsync();

                if(result)
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
