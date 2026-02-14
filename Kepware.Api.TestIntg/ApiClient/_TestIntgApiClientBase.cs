using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Kepware.Api.Model;
using Kepware.Api.Serializer;

namespace Kepware.Api.TestIntg.ApiClient
{
    public abstract class TestIntgApiClientBase
    {
        protected string TEST_ENDPOINT = "http://localhost:57412";

        protected readonly KepwareApiClient _kepwareApiClient;
        protected readonly KepwareApiClient _badCredKepwareApiClient;
        protected readonly ProductInfo _productInfo;

        protected TestIntgApiClientBase()
        {

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            TEST_ENDPOINT = $"{configuration["TestSettings:TestServer:Host"]}:{configuration["TestSettings:TestServer:Port"]}" ?? TEST_ENDPOINT;

            var testHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Add application services
                    services.AddKepwareApiClient(
                        name: "TestClient",
                        baseUrl: TEST_ENDPOINT,
                        apiUserName: $"{configuration["TestSettings:TestServer:UserName"]}",
                        apiPassword: $"{configuration["TestSettings:TestServer:Password"]}",
                        disableCertificateValidation: true
                        );
                    // Add application services
                    services.AddKepwareApiClient(
                        name: "BadCredClient",
                        baseUrl: TEST_ENDPOINT,
                        apiUserName: $"{configuration["TestSettings:TestServer:UserName"]}",
                        apiPassword: $"Test1234567890",
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
            var listClients = testHost.Services.GetServices<KepwareApiClient>();
            _kepwareApiClient = listClients.First(client => client.ClientName == "TestClient");
            _badCredKepwareApiClient = listClients.First(client => client.ClientName == "BadCredClient");

            // 3. Get the ProductInfo

            // Update the assignment to properly await the asynchronous method and handle the nullable return type.
            var connected = _kepwareApiClient.TestConnectionAsync().GetAwaiter().GetResult();
            _productInfo = _kepwareApiClient.GetProductInfoAsync().GetAwaiter().GetResult() ?? new ProductInfo();

        }

        protected static async Task<JsonProjectRoot> LoadJsonTestDataAsync()
        {
            var json = await File.ReadAllTextAsync("_data/simdemo_en-us.json");
            return JsonSerializer.Deserialize(json, KepJsonContext.Default.JsonProjectRoot)!;
        }

        protected Channel CreateTestChannel(string name = "TestChannel", string driver = "Simulator")
        {
            var channel = new Channel { Name = name };
            channel.SetDynamicProperty("servermain.MULTIPLE_TYPES_DEVICE_DRIVER", driver);
            channel.SetDynamicProperty("common.ALLTYPES_DESCRIPTION", "Example Simulator Channel");
            return channel;
        }

        protected async Task<Channel> AddTestChannel(string name = "TestChannel", string driver = "Simulator")
        {
            var channel = CreateTestChannel(name, driver);
            return await _kepwareApiClient.Project.Channels.GetOrCreateChannelAsync(channel.Name, channel.DeviceDriver!);
        }
        protected async Task DeleteAllChannelsAsync()
        {
            var channels = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<ChannelCollection, Channel>();
            if (channels != null)
            {
                foreach (var channel in channels)
                {
                    await _kepwareApiClient.Project.Channels.DeleteChannelAsync(channel.Name);
                }
            }
        }

        protected Device CreateTestDevice(Channel owner, string name = "TestDevice", string driver = "Simulator")
        {
            var device = new Device { Name = name, Owner = owner };
            device.SetDynamicProperty("servermain.MULTIPLE_TYPES_DEVICE_DRIVER", driver);
            device.SetDynamicProperty("common.ALLTYPES_DESCRIPTION", "Example Simulator Device");
            return device;
        }

        protected async Task<Device> AddTestDevice(Channel owner, string name = "TestDevice", string driver = "Simulator")
        {
            var device = CreateTestDevice(owner, name, driver);
            return await _kepwareApiClient.Project.Devices.GetOrCreateDeviceAsync(owner, device.Name);
        }

        protected async Task<Device> AddAtgTestDevice(Channel owner, string name = "TestDevice")
        {
            var device = new Device { Name = name, Owner = owner };
            device.SetDynamicProperty("servermain.MULTIPLE_TYPES_DEVICE_DRIVER", owner.DeviceDriver);
            device.SetDynamicProperty("common.ALLTYPES_DESCRIPTION", "Example Simulator Device");
            device.SetDynamicProperty("servermain.DEVICE_MODEL", 0);
            device.SetDynamicProperty("servermain.DEVICE_ID_STRING", "10.10.10.10,1,0");

            await _kepwareApiClient.GenericConfig.InsertItemAsync<Device>(device, owner);

            return device;
        }

        protected Tag CreateTestTag(string name = "Tag1", string address = "K0001")
        {
            return new Tag { Name = name, TagAddress = address };
        }

        protected async Task<Tag> AddSimulatorTestTag(Device owner, string name = "Tag1", string address = "K0001")
        {
            var tag = CreateTestTag(name, address);
            tag.Owner = owner;
            await _kepwareApiClient.GenericConfig.InsertItemAsync<DeviceTagCollection, Tag>(tag, owner);
            var tagFromApi = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Tag>(tag.Name, owner) ?? throw new Exception($"Failed to load tag '{tag.Name}' after insertion.") ;
            return tagFromApi ;
        }

        protected async Task<Tag> AddSimulatorTestTag(DeviceTagGroup owner, string name = "Tag1", string address = "K0001")
        {
            var tag = CreateTestTag(name, address);
            tag.Owner = owner;
            await _kepwareApiClient.GenericConfig.InsertItemAsync<DeviceTagCollection, Tag>(tag, owner);
            return await _kepwareApiClient.GenericConfig.LoadEntityAsync<Tag>(tag.Name, owner) ?? throw new Exception($"Failed to load tag '{tag.Name}' after insertion.") ;
        }

        protected List<Tag> CreateSimulatorTestTags(string name = "Tag", string address = "K000", int count = 2)
        {
            return Enumerable.Range(1, count)
                .Select(i => CreateTestTag(name: $"{name}{i}", address: $"{address}{i}"))
                .ToList();
        }

        protected async Task<List<Tag>> AddSimulatorTestTags(Device owner, string name = "Tag", string address = "K000", int count = 2)
        {
            var tagsList = CreateSimulatorTestTags(name, address);
            foreach (var tag in tagsList)
            {
                tag.Owner = owner;
            }
            await _kepwareApiClient.GenericConfig.InsertItemsAsync<DeviceTagCollection, Tag>(tagsList, owner: owner);
            return tagsList;
        }

        protected async Task<List<Tag>> AddSimulatorTestTags(DeviceTagGroup owner, string name = "Tag1", string address = "K000", int count = 2)
        {
            var tagsList = CreateSimulatorTestTags(name, address);
            foreach (var tag in tagsList)
            {
                tag.Owner = owner;
            }
            await _kepwareApiClient.GenericConfig.InsertItemsAsync<DeviceTagCollection, Tag>(tagsList, owner: owner);
            return tagsList;
        }

        protected DeviceTagGroup CreateTestTagGroup(string name = "TagGroup1")
        {
            return new DeviceTagGroup { Name = name };

        }
        protected DeviceTagGroup CreateTestTagGroup(Device owner, string name = "TagGroup1")
        {
            return new DeviceTagGroup(name: name, owner);

        }
        protected DeviceTagGroup CreateTestTagGroup(DeviceTagGroup owner, string name = "TagGroup1")
        {
            return new DeviceTagGroup(name: name, owner);

        }
        protected async Task<DeviceTagGroup> AddTestTagGroup(Device owner, string name = "TagGroup1", string driver = "Simulator")
        {

            var tagGroup = new DeviceTagGroup();
            tagGroup.Name = name;
            tagGroup.Owner = owner;
            await _kepwareApiClient.GenericConfig.InsertItemAsync<DeviceTagGroup>(tagGroup, tagGroup.Owner);
            return await _kepwareApiClient.GenericConfig.LoadEntityAsync<DeviceTagGroup>(tagGroup.Name, owner) ?? throw new Exception($"Failed to load tag group '{tagGroup.Name}' after insertion.");
            
        }

        protected async Task<DeviceTagGroup> AddTestTagGroup(DeviceTagGroup owner, string name = "TagGroup1", string driver = "Simulator")
        {

            var tagGroup = new DeviceTagGroup();
            tagGroup.Name = name;
            tagGroup.Owner = owner;
            await _kepwareApiClient.GenericConfig.InsertItemAsync<DeviceTagGroup>(tagGroup, tagGroup.Owner);
            return await _kepwareApiClient.GenericConfig.LoadEntityAsync<DeviceTagGroup>(tagGroup.Name, owner) ?? throw new Exception($"Failed to load tag group '{tagGroup.Name}' after insertion.");

        }
    }
}