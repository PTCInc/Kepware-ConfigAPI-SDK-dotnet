using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.IO;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.ClientHandler;

namespace Kepware.Api.Test.ApiClient
{
    public abstract class TestApiClientBase
    {
        protected const string TEST_ENDPOINT = "http://localhost:57412";

        protected readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        protected readonly Mock<ILogger<KepwareApiClient>> _loggerMock;
        protected readonly Mock<ILogger<AdminApiHandler>> _loggerMockAdmin;
        protected readonly Mock<ILogger<ProjectApiHandler>> _loggerMockProject;
        protected readonly Mock<ILogger<GenericApiHandler>> _loggerMockGeneric;
        protected readonly Mock<ILoggerFactory> _loggerFactoryMock;
        protected readonly KepwareApiClient _kepwareApiClient;

        /// <summary>
        /// URIs used to optimize recursion endpoints for tests. Populated via <see cref="ConfigureOptimizedRecursionUris(System.Collections.Generic.IEnumerable{string}?)"/>.
        /// </summary>
        protected readonly List<string> _optimizedRecursionUris = new List<string>();

        protected TestApiClientBase()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _loggerMock = new Mock<ILogger<KepwareApiClient>>();
            _loggerMockAdmin = new Mock<ILogger<AdminApiHandler>>();
            _loggerMockGeneric = new Mock<ILogger<GenericApiHandler>>();
            _loggerMockProject = new Mock<ILogger<ProjectApiHandler>>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();

            _loggerFactoryMock.Setup(factory => factory.CreateLogger(It.IsAny<string>())).Returns((string name) =>
            {
                if (name == typeof(KepwareApiClient).FullName)
                    return _loggerMock.Object;
                else if (name == typeof(AdminApiHandler).FullName)
                    return _loggerMockAdmin.Object;
                else if (name == typeof(GenericApiHandler).FullName)
                    return _loggerMockGeneric.Object;
                else if (name == typeof(ProjectApiHandler).FullName)
                    return _loggerMockProject.Object;
                else
                    return Mock.Of<ILogger>();
            });

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(TEST_ENDPOINT)
            };

            _kepwareApiClient = new KepwareApiClient("TestClient", new KepwareApiClientOptions { HostUri = httpClient.BaseAddress }, _loggerFactoryMock.Object, httpClient);
        }

        protected static async Task<JsonProjectRoot> LoadJsonTestDataAsync(string filePath = "_data/simdemo_en-us.json")
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<JsonProjectRoot>(json, KepJsonContext.Default.JsonProjectRoot)!;
        }

        protected async Task ConfigureToServeDrivers()
        {
            var jsonData = await File.ReadAllTextAsync("_data/doc_drivers.json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/doc/drivers/")
                .ReturnsResponse(HttpStatusCode.OK, jsonData, "application/json");
        }

        protected async Task ConfigureToServeSimDriver()
        {
            // Mock the simulator driver channels endpoint
            var jsonData = await File.ReadAllTextAsync("_data/simDriverChannelDef.json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/doc/drivers/Simulator/channels/")
                .ReturnsResponse(HttpStatusCode.OK, jsonData, "application/json");
        }

        protected void ConfigureConnectedClient(
            string productName = "KEPServerEX", 
            string productId = "012",
            int majorVersion = 6, 
            int minorVersion = 17, 
            int buildVersion = 240, 
            int patchVersion = 0)
        {
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/about")
                .ReturnsResponse(HttpStatusCode.OK, $$"""
                    {
                        "product_name": "{{productName}}",
                        "product_id": "{{productId}}",
                        "product_version": "V{{majorVersion}}.{{minorVersion}}.{{buildVersion}}.{{patchVersion}}",
                        "product_version_major": {{majorVersion}},
                        "product_version_minor": {{minorVersion}},
                        "product_version_build": {{buildVersion}},
                        "product_version_patch": {{patchVersion}}
                    }
                    """, "application/json");

            // Mock for the status endpoint
            var statusResponse = "[{\"Name\": \"ConfigAPI REST Service\", \"Healthy\": true}]";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/status")
                .ReturnsResponse(HttpStatusCode.OK, statusResponse, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/doc")
                .ReturnsResponse(HttpStatusCode.OK, statusResponse, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}/config/v1/project")
                .ReturnsResponse(HttpStatusCode.OK, "[]", "application/json");
        }

        protected Channel CreateTestChannel(string name = "TestChannel", string driver = "Advanced Simulator")
        {
            var channel = new Channel { Name = name };
            channel.SetDynamicProperty("servermain.MULTIPLE_TYPES_DEVICE_DRIVER", driver);
            return channel;
        }

        protected Device CreateTestDevice(Channel owner, string name = "TestDevice", string driver = "Advanced Simulator")
        {
            var device = new Device { Name = name, Owner = owner };
            device.SetDynamicProperty("servermain.MULTIPLE_TYPES_DEVICE_DRIVER", driver);
            return device;
        }

        protected List<Tag> CreateTestTags(int count = 2)
        {
            return Enumerable.Range(1, count)
                .Select(i => new Tag { Name = $"Tag{i}" })
                .ToList();
        }

        private const string ENDPONT_FULL_PROJECT = "/config/v1/project?content=serialize";
        protected async Task ConfigureToServeFullProject(string filePath = "_data/simdemo_en-us.json")
        {
            var jsonData = await File.ReadAllTextAsync(filePath);
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + ENDPONT_FULL_PROJECT)
                                   .ReturnsResponse(jsonData, "application/json");
        }

        protected async Task ConfigureToServeEndpoints(string filePath = "_data/simdemo_en-us.json")
        {
            var projectData = await LoadJsonTestDataAsync(filePath);


            var channels = projectData.Project?.Channels?
                .Select(c =>
                {
                    var ch = new Channel { Name = c.Name, Description = c.Description, DynamicProperties = c.DynamicProperties };
                    int staticCount = c.Name switch
                    {
                        "Channel1" => 2,
                        "Simulation Examples" => 24,
                        "Data Type Examples" => 216,
                        "OptRecursionTest" => 318,
                        _ => 0
                    };
                    ch.SetDynamicProperty(Properties.Channel.StaticTagCount, staticCount);
                    return ch;
                }).ToList() ?? new List<Channel>();

            // Serve project details
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project")
                                   .ReturnsResponse(JsonSerializer.Serialize(new Project { Description = projectData?.Project?.Description, DynamicProperties = projectData?.Project?.DynamicProperties ?? [] }), "application/json");

            // Serve channels without nested devices
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels")
                                   .ReturnsResponse(JsonSerializer.Serialize(channels), "application/json");

            foreach (var channel in projectData?.Project?.Channels ?? [])
            {
                _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}")
                                       .ReturnsResponse(JsonSerializer.Serialize(new Channel { Name = channel.Name, Description = channel.Description, DynamicProperties = channel.DynamicProperties }), "application/json");

                if (channel.Devices != null)
                {
                    var devices = channel.Devices
                        .Select(d =>
                        {
                            var dev = new Device { Name = d.Name, Description = d.Description, DynamicProperties = d.DynamicProperties };
                            int staticCount = d.Name switch
                            {
                                "Device1" => 2,
                                "Functions" => 24,
                                "16 Bit Device" => 98,
                                "8 Bit Device" => 118,
                                "RecursionTestDevice" => 318,
                                _ => 0
                            };
                            dev.SetDynamicProperty(Properties.Device.StaticTagCount, staticCount);
                            return dev;
                        }).ToList() ?? new List<Device>();
                    _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices")
                                           .ReturnsResponse(JsonSerializer.Serialize(devices), "application/json");

                    foreach (var device in channel.Devices)
                    {
                        var deviceEndpoint = TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}";


                        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, deviceEndpoint)
                                               .ReturnsResponse(JsonSerializer.Serialize(new Device { Name = device.Name, Description = device.Description, DynamicProperties = device.DynamicProperties }), "application/json");


                        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, deviceEndpoint + "/tags")
                                              .ReturnsResponse(JsonSerializer.Serialize(device.Tags), "application/json");

                        ConfigureToServeEndpointsTagGroupsRecursive(deviceEndpoint, device.TagGroups ?? []);
                    }
                }
            }

            // Additional endpoints for content=serialize mocking
            var projectPropertiesString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/projectProperties.json");
            var channel1String = await File.ReadAllTextAsync("_data/projectLoadSerializeData/channel1.json");
            var sixteenBitDeviceString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/dataTypeExamples.16BitDevice.json");
            var simExamplesChannelString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/simulationExamples.json");
            var dte8BitBRegTagGroupString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/dte.8bitDevice.Breg.json");
            var dte8BitKRegTagGroupString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/dte.8bitDevice.Kreg.json");
            var dte8BitRRegTagGroupString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/dte.8bitDevice.Rreg.json");
            var dte8BitSRegTagGroupString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/dte.8bitDevice.Sreg.json");
            var optRecursionDeviceBRegTagGroupString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/opt.recursionDevice.Breg.json");
            var optRecursionDeviceKRegTagGroupString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/opt.recursionDevice.Kreg.json");
            var optRecursionDeviceRRegTagGroupString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/opt.recursionDevice.Rreg.json");
            var optRecursionDeviceSRegTagGroupString = await File.ReadAllTextAsync("_data/projectLoadSerializeData/opt.recursionDevice.Sreg.json");
            var optRecursionDeviceRecursionTestLevel1String = await File.ReadAllTextAsync("_data/projectLoadSerializeData/opt.recursionDevice.RecursionTest.Level1.json");
            var optRecursionDeviceRecursionTestLevel2String = await File.ReadAllTextAsync("_data/projectLoadSerializeData/opt.recursionDevice.RecursionTest.Level2.json");
            var optRecursionDeviceRecursionTestLevel3String = await File.ReadAllTextAsync("_data/projectLoadSerializeData/opt.recursionDevice.RecursionTest.Level3.json");
            var optRecursionDeviceRecursionTestLevel4String = await File.ReadAllTextAsync("_data/projectLoadSerializeData/opt.recursionDevice.RecursionTest.Level4.json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project")
                                    .ReturnsResponse(projectPropertiesString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Channel1?content=serialize")
                                    .ReturnsResponse(channel1String, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/Channel1?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/16 Bit Device?content=serialize")
                                    .ReturnsResponse(sixteenBitDeviceString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/16 Bit Device?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Simulation Examples?content=serialize")
                                    .ReturnsResponse(simExamplesChannelString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/Simulation Examples?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/8 Bit Device/tag_groups/B Registers?content=serialize")
                                    .ReturnsResponse(dte8BitBRegTagGroupString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/8 Bit Device/tag_groups/B Registers?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/8 Bit Device/tag_groups/K Registers?content=serialize")
                                    .ReturnsResponse(dte8BitKRegTagGroupString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/8 Bit Device/tag_groups/K Registers?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/8 Bit Device/tag_groups/R Registers?content=serialize")
                                    .ReturnsResponse(dte8BitRRegTagGroupString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/8 Bit Device/tag_groups/R Registers?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/8 Bit Device/tag_groups/S Registers?content=serialize")
                                    .ReturnsResponse(dte8BitSRegTagGroupString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/Data Type Examples/devices/8 Bit Device/tag_groups/S Registers?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/B Registers?content=serialize")
                                    .ReturnsResponse(optRecursionDeviceBRegTagGroupString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/B Registers?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/K Registers?content=serialize")
                                    .ReturnsResponse(optRecursionDeviceKRegTagGroupString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/K Registers?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/R Registers?content=serialize")
                                    .ReturnsResponse(optRecursionDeviceRRegTagGroupString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/R Registers?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/S Registers?content=serialize")
                                    .ReturnsResponse(optRecursionDeviceSRegTagGroupString, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/S Registers?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/RecursionTest/tag_groups/Level1?content=serialize")
                                    .ReturnsResponse(optRecursionDeviceRecursionTestLevel1String, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/RecursionTest/tag_groups/Level1?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/RecursionTest/tag_groups/Level2?content=serialize")
                                    .ReturnsResponse(optRecursionDeviceRecursionTestLevel2String, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/RecursionTest/tag_groups/Level2?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/RecursionTest/tag_groups/Level3?content=serialize")
                                    .ReturnsResponse(optRecursionDeviceRecursionTestLevel3String, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/RecursionTest/tag_groups/Level3?content=serialize");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/RecursionTest/tag_groups/Level4?content=serialize")
                                    .ReturnsResponse(optRecursionDeviceRecursionTestLevel4String, "application/json");
            _optimizedRecursionUris.Add(TEST_ENDPOINT + "/config/v1/project/channels/OptRecursionTest/devices/RecursionTestDevice/tag_groups/RecursionTest/tag_groups/Level4?content=serialize");

        }
        private void ConfigureToServeEndpointsTagGroupsRecursive(string endpoint, IEnumerable<DeviceTagGroup> tagGroups)
        {
            var tagGroupEndpoint = endpoint + "/tag_groups";

            var updatedTagGroups = tagGroups
                .Select(tg =>
                {
                    var tagGrp = tg;
                    int staticCount = tg.Name switch
                    {
                        "B Registers" => 5,
                        "K Registers" => 54,
                        "R Registers" => 54,
                        "S Registers" => 5,
                        "RecursionTest" => 220,
                        "Level1" => 88,
                        "Level2" => 44,
                        "Level3" => 44,
                        "Level4" => 44,
                        "Level1_1" => 44,
                        _ => 0
                    };
                    tagGrp.SetDynamicProperty(Properties.DeviceTagGroup.TotalTagCount, staticCount);
                    return tagGrp;
                }).ToList() ?? new List<DeviceTagGroup>();

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, tagGroupEndpoint)
                                             .ReturnsResponse(JsonSerializer.Serialize(updatedTagGroups), "application/json");

            foreach (var tagGroup in updatedTagGroups)
            {
                _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, string.Concat(tagGroupEndpoint, "/", tagGroup.Name, "/tags"))
                                             .ReturnsResponse(JsonSerializer.Serialize(tagGroup.Tags), "application/json");

                ConfigureToServeEndpointsTagGroupsRecursive(string.Concat(tagGroupEndpoint, "/", tagGroup.Name), tagGroup.TagGroups ?? []);
            }
        }
    }
}
