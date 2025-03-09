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
            var result = await _projectApiHandler.GetOrCreateChannelAsync(channelName, "Simulator");

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
            var result = await _projectApiHandler.GetOrCreateChannelAsync(channelName, driverName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(channelName, result.Name);
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
            var result = await _projectApiHandler.UpdateChannelAsync(channel);

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
            var result = await _projectApiHandler.DeleteChannelAsync(channel);

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
            var result = await _projectApiHandler.GetOrCreateDeviceAsync(channel, deviceName);

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
            var result = await _projectApiHandler.GetOrCreateDeviceAsync(channel, deviceName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(deviceName, result.Name);
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
            var result = await _projectApiHandler.UpdateDeviceAsync(device);

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
            var result = await _projectApiHandler.DeleteDeviceAsync(device);

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
            var tagGroups = new List<DeviceTagGroup> { tagGroup };

            // Act
            await _projectApiHandler.LoadTagGroupsRecursiveAsync(tagGroups);

            // Assert
            Assert.NotNull(tagGroup.TagGroups);
            Assert.Single(tagGroup.TagGroups);
            Assert.Equal("TagGroup1", tagGroup.TagGroups.First().Name);
        }

        #endregion
    }
}

