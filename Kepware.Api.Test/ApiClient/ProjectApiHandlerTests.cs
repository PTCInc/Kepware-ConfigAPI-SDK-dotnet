using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kepware.Api.Model;
using Kepware.Api.ClientHandler;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;
using System.Text.Json;
using Shouldly;

namespace Kepware.Api.Test.ApiClient
{
    public class ProjectApiHandlerTests : TestApiClientBase
    {
        private readonly ProjectApiHandler _projectApiHandler;

        public ProjectApiHandlerTests()
        {
            _projectApiHandler = _kepwareApiClient.Project;
        }

        #region Channel Tests

        [Fact]
        public async Task GetOrCreateChannelAsync_ShouldReturnChannel_WhenChannelExists()
        {
            // Arrange
            var channelName = "ExistingChannel";
            var channelJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "ExistingChannel",
                    "common.ALLTYPES_DESCRIPTION": "Example Channel",
                    "servermain.MULTIPLE_TYPES_DEVICE_DRIVER": "Simulator"
                }
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channelName}")
                                   .ReturnsResponse(channelJson, "application/json");

            // Act
            var result = await _projectApiHandler.Channels.GetOrCreateChannelAsync(channelName, "Simulator");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(channelName, result.Name);
        }

        [Fact]
        public async Task GetOrCreateChannelAsync_ShouldCreateChannel_WhenChannelDoesNotExist()
        {
            // Arrange
            await ConfigureToServeDrivers();

            var channelName = "NewChannel";
            var driverName = "Simulator";
            var channelJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "NewChannel",
                    "common.ALLTYPES_DESCRIPTION": "Example Channel",
                    "servermain.MULTIPLE_TYPES_DEVICE_DRIVER": "Simulator"
                }
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channelName}")
                                   .ReturnsResponse(HttpStatusCode.NotFound);

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, TEST_ENDPOINT + "/config/v1/project/channels")
                                   .ReturnsResponse(channelJson, "application/json");

            // Act
            var result = await _projectApiHandler.Channels.GetOrCreateChannelAsync(channelName, driverName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(channelName, result.Name);
        }

        [Fact]
        public async Task GetChannelAsync_ShouldReturnChannel_WhenChannelExists()
        {
            // Arrange
            var channelName = "ExistingChannel";
            var channelJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "ExistingChannel",
                    "common.ALLTYPES_DESCRIPTION": "Example Channel",
                    "servermain.MULTIPLE_TYPES_DEVICE_DRIVER": "Simulator"
                }
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channelName}")
                                   .ReturnsResponse(channelJson, "application/json");

            // Act
            var result = await _projectApiHandler.Channels.GetChannelAsync(channelName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(channelName, result.Name);
        }

        [Fact]
        public async Task GetChannelAsync_ShouldReturnNull_WhenChannelDoesNotExist()
        {
            // Arrange
            await ConfigureToServeDrivers();

            var channelName = "NewChannel";

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channelName}")
                                   .ReturnsResponse(HttpStatusCode.NotFound);

            // Act
            var result = await _projectApiHandler.Channels.GetChannelAsync(channelName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateChannelAsync_ShouldReturnTrue_WhenUpdateIsSuccessful()
        {
            // Arrange
            await ConfigureToServeDrivers();
            var channelJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "ChannelToUpdate",
                    "common.ALLTYPES_DESCRIPTION": "Example Channel",
                    "servermain.MULTIPLE_TYPES_DEVICE_DRIVER": "Simulator"
                }
                """;

            var channel = new Channel { Name = "ChannelToUpdate" };

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}")
                 .ReturnsResponse(channelJson, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}")
                                   .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _projectApiHandler.Channels.UpdateChannelAsync(channel);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteChannelAsync_ShouldReturnTrue_WhenDeletionIsSuccessful()
        {
            // Arrange
            var channel = new Channel { Name = "ChannelToDelete" };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}")
                                   .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _projectApiHandler.Channels.DeleteChannelAsync(channel);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Device Tests

        [Fact]
        public async Task GetOrCreateDeviceAsync_ShouldReturnDevice_WhenDeviceExists()
        {
            // Arrange
            await ConfigureToServeDrivers();
            var channel = new Channel { Name = "ExistingChannel" };
            var deviceName = "ExistingDevice";
            var deviceJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "ExistingDevice",
                    "common.ALLTYPES_DESCRIPTION": "Example Device",
                    "servermain.DEVICE_CHANNEL_ASSIGNMENT": "ExistingChannel"
                }
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{deviceName}")
                                   .ReturnsResponse(deviceJson, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{deviceName}/tags")
                                .ReturnsResponse("[]", "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{deviceName}/tag_groups")
                                .ReturnsResponse("[]", "application/json");

            // Act
            var result = await _projectApiHandler.Devices.GetOrCreateDeviceAsync(channel, deviceName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceName, result.Name);
        }

        [Fact]
        public async Task GetOrCreateDeviceAsync_ShouldCreateDevice_WhenDeviceDoesNotExist()
        {
            // Arrange
            await ConfigureToServeDrivers();
            var channel = new Channel { Name = "ExistingChannel", DeviceDriver = "Simulator" };
            var deviceName = "NewDevice";
            var deviceJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "NewDevice",
                    "common.ALLTYPES_DESCRIPTION": "Example Device",
                    "servermain.DEVICE_CHANNEL_ASSIGNMENT": "ExistingChannel"
                }
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{deviceName}")
                                   .ReturnsResponse(HttpStatusCode.NotFound);

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices")
                                   .ReturnsResponse(deviceJson, "application/json");

            // Act
            var result = await _projectApiHandler.Devices.GetOrCreateDeviceAsync(channel, deviceName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceName, result.Name);
        }

        [Fact]
        public async Task GetDeviceAsync_ShouldReturnDevice_WhenDeviceExists()
        {
            // Arrange
            await ConfigureToServeDrivers();
            var channel = new Channel { Name = "ExistingChannel" };
            var deviceName = "ExistingDevice";
            var deviceJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "ExistingDevice",
                    "common.ALLTYPES_DESCRIPTION": "Example Device",
                    "servermain.DEVICE_CHANNEL_ASSIGNMENT": "ExistingChannel"
                }
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{deviceName}")
                                   .ReturnsResponse(deviceJson, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{deviceName}/tags")
                                .ReturnsResponse("[]", "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{deviceName}/tag_groups")
                                .ReturnsResponse("[]", "application/json");

            // Act
            var result = await _projectApiHandler.Devices.GetDeviceAsync(channel, deviceName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceName, result.Name);
        }

        [Fact]
        public async Task GetDeviceAsync_ShouldReturnNull_WhenDeviceDoesNotExist()
        {
            // Arrange
            await ConfigureToServeDrivers();
            var channel = new Channel { Name = "ExistingChannel", DeviceDriver = "Simulator" };
            var deviceName = "NewDevice";

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{deviceName}")
                                   .ReturnsResponse(HttpStatusCode.NotFound);

            // Act
            var result = await _projectApiHandler.Devices.GetDeviceAsync(channel, deviceName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateDeviceAsync_ShouldReturnTrue_WhenUpdateIsSuccessful()
        {
            // Arrange
            var deviceJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "DeviceToUpdate",
                    "common.ALLTYPES_DESCRIPTION": "Example Device",
                    "servermain.DEVICE_CHANNEL_ASSIGNMENT": "ExistingChannel"
                }
                """;
            var device = new Device { Name = "DeviceToUpdate", Channel = new Channel { Name = "ExistingChannel" } };

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{device.Channel.Name}/devices/{device.Name}")
                 .ReturnsResponse(deviceJson, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, TEST_ENDPOINT + $"/config/v1/project/channels/{device.Channel.Name}/devices/{device.Name}")
                                   .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _projectApiHandler.Devices.UpdateDeviceAsync(device);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteDeviceAsync_ShouldReturnTrue_WhenDeletionIsSuccessful()
        {
            // Arrange
            var device = new Device { Name = "DeviceToDelete", Channel = new Channel { Name = "ExistingChannel" } };
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, TEST_ENDPOINT + $"/config/v1/project/channels/{device.Channel.Name}/devices/{device.Name}")
                                   .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _projectApiHandler.Devices.DeleteDeviceAsync(device);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteDeviceAsync_ItemByName_ShouldReturnTrue_WhenDeletionIsSuccessful()
        {
            // Arrange
            var device = new Device { Name = "DeviceToDelete", Channel = new Channel { Name = "ExistingChannel" } };
            var endpoint = $"/config/v1/project/channels/{device.Channel.Name}/devices/{device.Name}";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, TEST_ENDPOINT + endpoint)
                                   .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var result = await _projectApiHandler.Devices.DeleteDeviceAsync(device.Channel.Name, device.Name);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Device Tag & Tag Group Tests

        [Fact]
        public async Task LoadTagGroupsRecursiveAsync_ShouldLoadTagGroupsCorrectly()
        {
            // Arrange
            await ConfigureToServeDrivers();
            var device = new Device { Name = "DeviceWithTags", Channel = new Channel { Name = "ExistingChannel" } };
            var tagGroupsJson = """
                [
                    {
                        "PROJECT_ID": 676550906,
                        "common.ALLTYPES_NAME": "TagGroup1",
                        "common.ALLTYPES_DESCRIPTION": "Example Tag Group",
                        "servermain.TAGGROUP_LOCAL_TAG_COUNT": 5,
                        "servermain.TAGGROUP_TOTAL_TAG_COUNT": 5,
                        "servermain.TAGGROUP_AUTOGENERATED": false
                    }
                ]
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{device.Channel.Name}/devices/{device.Name}/tag_groups")
                                   .ReturnsResponse(tagGroupsJson, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{device.Channel.Name}/devices/{device.Name}/tag_groups/TagGroup1/tags")
                        .ReturnsResponse("[]", "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{device.Channel.Name}/devices/{device.Name}/tag_groups/TagGroup1/tag_groups")
                        .ReturnsResponse(tagGroupsJson, "application/json");

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{device.Channel.Name}/devices/{device.Name}/tag_groups/TagGroup1/tag_groups/TagGroup1/tags")
                        .ReturnsResponse("[]", "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{device.Channel.Name}/devices/{device.Name}/tag_groups/TagGroup1/tag_groups/TagGroup1/tag_groups")
                        .ReturnsResponse("[]", "application/json");

            var tagGroup = new DeviceTagGroup { Name = "TagGroup1", Owner = device };
            var tagGroup2 = new DeviceTagGroup { Name = "TagGroup1", Owner = tagGroup };
            var tagGroups = new List<DeviceTagGroup> { tagGroup , tagGroup2 };

            // Act
            await ProjectApiHandler.LoadTagGroupsRecursiveAsync(_kepwareApiClient, tagGroups);

            // Assert
            Assert.NotNull(tagGroup.TagGroups);
            Assert.Single(tagGroup.TagGroups);
            Assert.Equal("TagGroup1", tagGroup.TagGroups.First().Name);
            Assert.NotNull(tagGroup2.TagGroups);
            Assert.Empty(tagGroup2.TagGroups);
        }

        #endregion

        #region CompareAndApply Tests

        [Theory]
        [InlineData("KEPServerEX", "12", 6, 17)]  // Supports JSON Project Load Service (6.17+)
        [InlineData("KEPServerEX", "12", 6, 16)] // Does not support it (6.16)
        [InlineData("ThingworxKepwareServer", "12", 6, 17)]  // Supports JSON Project Load Service (6.17+)
        [InlineData("ThingworxKepwareServer", "12", 6, 16)] // Does not support it (6.16)
        [InlineData("ThingWorxKepwareEdge", "13", 1, 10)] // Supports JSON Project Load Service (1.10+)
        [InlineData("ThingWorxKepwareEdge", "13", 1, 9)] // Does not support it (1.9)
        [InlineData("Kepware Edge", "13", 1, 0)] // Supports JSON Project Load Service
        [InlineData("UnknownProduct", "99", 10, 0)] // Unknown product, should be false
        public async Task CompareAndApply_ShouldReturn2Inserts1Update1Delete_WhenSourceHas2ChannelsAndModifiedProperties(
          string productName, string productId, int majorVersion, int minorVersion)
        {
            string filePath = "_data/sourceCompare.json";
            // Arrange
            ConfigureConnectedClient(productName, productId, majorVersion, minorVersion);
            var about = await _kepwareApiClient.GetProductInfoAsync();
            about.ShouldNotBeNull();

            if (about.SupportsJsonProjectLoadService)
            {
                await ConfigureToServeFullProject(filePath);
            }
            else
            {
                 await ConfigureToServeEndpoints(filePath);
            }

            await ConfigureToServeDrivers();
            await ConfigureToServeSimDriver();
            var projectJson = await LoadJsonTestDataAsync(filePath);

            // Mock project properties endpoint
            var test = JsonSerializer.Serialize(projectJson.Project?.DynamicProperties);
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project")
                                   .ReturnsResponse( test , "application/json")
                                   .Verifiable();

            // Mock channel creation POST requests
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, TEST_ENDPOINT + "/config/v1/project/channels")
                                   .ReturnsResponse(HttpStatusCode.Created)
                                   .Verifiable();

            // Mock channel deletion for the existing channel that's not in source
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, TEST_ENDPOINT + "/config/v1/project/channels/ExistingChannel")
                                   .ReturnsResponse(HttpStatusCode.OK)
                                   .Verifiable();

            // Mock project properties update
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, TEST_ENDPOINT + "/config/v1/project")
                                   .ReturnsResponse(HttpStatusCode.OK)
                                   .Verifiable();

            // Create source project with 2 new channels and modified properties
            var newProject = new Project
            {
                Description = "Modified Project Description",
                Channels = new ChannelCollection
                {
                    new Channel { Name = "NewChannel1", DynamicProperties = new Dictionary<string, JsonElement> { { "servermain.CHANNEL_UNIQUE_ID", JsonDocument.Parse($"592352578").RootElement }, {"servermain.MULTIPLE_TYPES_DEVICE_DRIVER", JsonDocument.Parse($"\"Simulator\"").RootElement} } },
                    new Channel { Name = "NewChannel2", DynamicProperties = new Dictionary<string, JsonElement> { { "servermain.CHANNEL_UNIQUE_ID", JsonDocument.Parse($"592352577").RootElement }, {"servermain.MULTIPLE_TYPES_DEVICE_DRIVER", JsonDocument.Parse($"\"Simulator\"").RootElement} } }
                }
            };
            newProject.SetDynamicProperty("uaserverinterface.PROJECT_OPC_UA_ANONYMOUS_LOGIN", false);

            // Act
            var result = await _projectApiHandler.CompareAndApply(newProject);

            // Assert
            Assert.Equal(2, result.inserts);  // 2 new channels added
            Assert.Equal(1, result.updates);  // 1 project property update
            Assert.Equal(1, result.deletes);  // 1 existing channel deleted
        }

        #endregion
    }
}

