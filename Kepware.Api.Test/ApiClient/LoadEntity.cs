using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kepware.Api.Model;
using Kepware.Api.Test.ApiClient;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;

namespace Kepware.Api.Test.ApiClient
{
    public class LoadEntityTests : TestApiClientBase
    {
        #region LoadEntityAsync - Channel (Collection)

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnChannelCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var channelsJson = """
                [
                    {
                        "PROJECT_ID": 676550906,
                        "common.ALLTYPES_NAME": "Channel1",
                        "common.ALLTYPES_DESCRIPTION": "Example Simulator Channel",
                        "servermain.MULTIPLE_TYPES_DEVICE_DRIVER": "Simulator"
                    },
                    {
                        "PROJECT_ID": 676550906,
                        "common.ALLTYPES_NAME": "Data Type Examples",
                        "common.ALLTYPES_DESCRIPTION": "Example Simulator Channel",
                        "servermain.MULTIPLE_TYPES_DEVICE_DRIVER": "Simulator"
                    }
                ]
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels")
                                   .ReturnsResponse(channelsJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<ChannelCollection, Channel>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.Name == "Channel1");
            Assert.Contains(result, c => c.Name == "Data Type Examples");
        }

        #endregion

        #region LoadEntityAsync - Single Channel

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnChannel_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var channelJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "Channel1",
                    "common.ALLTYPES_DESCRIPTION": "Example Simulator Channel",
                    "servermain.MULTIPLE_TYPES_DEVICE_DRIVER": "Simulator"
                }
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Channel1")
                                   .ReturnsResponse(channelJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>("Channel1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Channel1", result.Name);
            Assert.Equal("Example Simulator Channel", result.Description);
        }

        #endregion

        #region LoadEntityAsync - Device (Collection)

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnDeviceCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var devicesJson = """
                [
                    {
                        "PROJECT_ID": 676550906,
                        "common.ALLTYPES_NAME": "16 Bit Device",
                        "common.ALLTYPES_DESCRIPTION": "Example Simulator Device",
                        "servermain.DEVICE_CHANNEL_ASSIGNMENT": "Data Type Examples"
                    },
                    {
                        "PROJECT_ID": 676550906,
                        "common.ALLTYPES_NAME": "8 Bit Device",
                        "common.ALLTYPES_DESCRIPTION": "Example Simulator Device",
                        "servermain.DEVICE_CHANNEL_ASSIGNMENT": "Data Type Examples"
                    }
                ]
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data%20Type%20Examples/devices")
                                   .ReturnsResponse(devicesJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceCollection, Device>(["Data Type Examples"]);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, d => d.Name == "16 Bit Device");
            Assert.Contains(result, d => d.Name == "8 Bit Device");
        }

        #endregion

        #region LoadEntityAsync - Single Device

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnDevice_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var deviceJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "16 Bit Device",
                    "common.ALLTYPES_DESCRIPTION": "Example Simulator Device",
                    "servermain.DEVICE_CHANNEL_ASSIGNMENT": "Data Type Examples"
                }
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device")
                                   .ReturnsResponse(deviceJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Device>("16 Bit Device", new NamedEntity { Name = "Data Type Examples" });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("16 Bit Device", result.Name);
            Assert.Equal("Example Simulator Device", result.Description);
            Assert.Equal("Data Type Examples", result.Owner?.Name);
        }

        #endregion

        #region LoadEntityAsync - Error Handling

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnNull_WhenApiReturns404()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/InvalidChannel")
                                   .ReturnsResponse(HttpStatusCode.NotFound);

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>("InvalidChannel");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnNull_WhenApiReturns500()
        {
            // Arrange
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Channel1")
                                   .ReturnsResponse(HttpStatusCode.InternalServerError);

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>("Channel1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnNull_OnHttpRequestException()
        {
            // Arrange
            _httpMessageHandlerMock.SetupAnyRequest()
                                   .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>("Channel1");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region LoadEntityAsync - Device with DynamicProperties
        // TODO: Add these types of test for other entities (Device, TagGroup, Tag, etc.)
        [Fact]
        public async Task LoadEntityAsync_ShouldReturnDevice_WithCorrectDynamicProperties()
        {
            // Arrange
            var deviceJson = """
                {
                    "PROJECT_ID": 676550906,
                    "common.ALLTYPES_NAME": "16 Bit Device",
                    "common.ALLTYPES_DESCRIPTION": "Example Simulator Device",
                    "servermain.DEVICE_CHANNEL_ASSIGNMENT": "Data Type Examples",
                    "servermain.DEVICE_ID_FORMAT": 1,
                    "servermain.DEVICE_ID_STRING": "3",
                    "servermain.DEVICE_ID_HEXADECIMAL": 3,
                    "servermain.DEVICE_ID_DECIMAL": 3,
                    "servermain.DEVICE_ID_OCTAL": 3,
                    "servermain.DEVICE_DATA_COLLECTION": true,
                    "servermain.DEVICE_STATIC_TAG_COUNT": 98,
                    "servermain.DEVICE_SCAN_MODE": 0,
                    "servermain.DEVICE_SCAN_MODE_RATE_MS": 1000,
                    "servermain.DEVICE_SCAN_MODE_PROVIDE_INITIAL_UPDATES_FROM_CACHE": false
                }
                """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device")
                                   .ReturnsResponse(deviceJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Device>("16 Bit Device", new NamedEntity { Name = "Data Type Examples" });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("16 Bit Device", result.Name);
            Assert.Equal("Example Simulator Device", result.Description);
            Assert.Equal("Data Type Examples", result.Owner?.Name);

            // Check if DynamicProperties are set correctly
            Assert.Equal(1, result.GetDynamicProperty<int>("servermain.DEVICE_ID_FORMAT"));
            Assert.Equal("3", result.GetDynamicProperty<string>("servermain.DEVICE_ID_STRING"));
            Assert.Equal(3, result.GetDynamicProperty<int>("servermain.DEVICE_ID_HEXADECIMAL"));
            Assert.Equal(3, result.GetDynamicProperty<int>("servermain.DEVICE_ID_DECIMAL"));
            Assert.Equal(3, result.GetDynamicProperty<int>("servermain.DEVICE_ID_OCTAL"));
            Assert.True(result.GetDynamicProperty<bool>("servermain.DEVICE_DATA_COLLECTION"));
            Assert.Equal(98, result.GetDynamicProperty<int>("servermain.DEVICE_STATIC_TAG_COUNT"));
            Assert.Equal(0, result.GetDynamicProperty<int>("servermain.DEVICE_SCAN_MODE"));
            Assert.Equal(1000, result.GetDynamicProperty<int>("servermain.DEVICE_SCAN_MODE_RATE_MS"));
            Assert.False(result.GetDynamicProperty<bool>("servermain.DEVICE_SCAN_MODE_PROVIDE_INITIAL_UPDATES_FROM_CACHE"));
        }

        #endregion

        #region LoadEntityAsync - TagGroup (Collection)

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTagGroupCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var tagGroupsJson = """
    [
        {
            "PROJECT_ID": 676550906,
            "common.ALLTYPES_NAME": "B Registers",
            "common.ALLTYPES_DESCRIPTION": "Boolean registers",
            "servermain.TAGGROUP_LOCAL_TAG_COUNT": 5,
            "servermain.TAGGROUP_TOTAL_TAG_COUNT": 5,
            "servermain.TAGGROUP_AUTOGENERATED": false
        },
        {
            "PROJECT_ID": 676550906,
            "common.ALLTYPES_NAME": "K Registers",
            "common.ALLTYPES_DESCRIPTION": "Constant Registers",
            "servermain.TAGGROUP_LOCAL_TAG_COUNT": 44,
            "servermain.TAGGROUP_TOTAL_TAG_COUNT": 44,
            "servermain.TAGGROUP_AUTOGENERATED": false
        }
    ]
    """;
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device/tag_groups")
                                   .ReturnsResponse(tagGroupsJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(new Device("16 Bit Device", "Data Type Examples"));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, g => g.Name == "B Registers");
            Assert.Contains(result, g => g.Name == "K Registers");
        }

        #endregion

        #region LoadEntityAsync - Single TagGroup

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTagGroup_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var tagGroupJson = """
    {
        "PROJECT_ID": 676550906,
        "common.ALLTYPES_NAME": "B Registers",
        "common.ALLTYPES_DESCRIPTION": "Boolean registers",
        "servermain.TAGGROUP_LOCAL_TAG_COUNT": 5,
        "servermain.TAGGROUP_TOTAL_TAG_COUNT": 5,
        "servermain.TAGGROUP_AUTOGENERATED": false
    }
    """;
            // /config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device/config/v1/project/channels/B Registers/devices/B Registers
            // /config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device/tag_groups/B%20Registers
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device/tag_groups/B%20Registers")
                                   .ReturnsResponse(tagGroupJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<DeviceTagGroup>("B Registers", new Device("16 Bit Device", "Data Type Examples"));

            // Assert
            Assert.NotNull(result);
            Assert.Equal("B Registers", result.Name);
            Assert.Equal("Boolean registers", result.Description);
            Assert.False(result.GetDynamicProperty<bool>("servermain.TAGGROUP_AUTOGENERATED"));
            Assert.Equal(5, result.GetDynamicProperty<int>("servermain.TAGGROUP_LOCAL_TAG_COUNT"));
            Assert.Equal(5, result.GetDynamicProperty<int>("servermain.TAGGROUP_TOTAL_TAG_COUNT"));
        }

        #endregion

        #region LoadEntityAsync - Tag (Collection)

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTagCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var tagsJson = """
    [
        {
            "PROJECT_ID": 676550906,
            "common.ALLTYPES_NAME": "Boolean1",
            "common.ALLTYPES_DESCRIPTION": "Boolean register",
            "servermain.TAG_ADDRESS": "B0001",
            "servermain.TAG_DATA_TYPE": 1
        },
        {
            "PROJECT_ID": 676550906,
            "common.ALLTYPES_NAME": "Boolean2",
            "common.ALLTYPES_DESCRIPTION": "Boolean register",
            "servermain.TAG_ADDRESS": "B0002",
            "servermain.TAG_DATA_TYPE": 1
        }
    ]
    """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device/tag_groups/B%20Registers/tags")
                                   .ReturnsResponse(tagsJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupTagCollection, Tag>(new DeviceTagGroup("B Registers", new Device("16 Bit Device", "Data Type Examples")));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Name == "Boolean1");
            Assert.Contains(result, t => t.Name == "Boolean2");
        }

        #endregion

        #region LoadEntityAsync - Exception Fall

        [Fact]
        public async Task LoadEntityAsync_ShouldThrowInvalidOperationException_WhenLoadRecursiveEndpointWithStringList()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupTagCollection, Tag>(["Data Type Examples", "16 Bit Device", "B Registers"]);
            });

            Assert.Equal("Recursive endpoint does not support string list item name", exception.Message);
        }

        #endregion

        #region LoadEntityAsync - Single Tag mit DynamicProperties

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTag_WithCorrectDynamicProperties()
        {
            // Arrange
            var tagJson = """
    {
        "PROJECT_ID": 676550906,
        "common.ALLTYPES_NAME": "Boolean1",
        "common.ALLTYPES_DESCRIPTION": "Boolean register",
        "servermain.TAG_ADDRESS": "B0001",
        "servermain.TAG_DATA_TYPE": 1,
        "servermain.TAG_READ_WRITE_ACCESS": 1,
        "servermain.TAG_SCAN_RATE_MILLISECONDS": 100,
        "servermain.TAG_AUTOGENERATED": false,
        "servermain.TAG_SCALING_TYPE": 0,
        "servermain.TAG_SCALING_RAW_LOW": 0,
        "servermain.TAG_SCALING_RAW_HIGH": 1000,
        "servermain.TAG_SCALING_SCALED_DATA_TYPE": 9,
        "servermain.TAG_SCALING_SCALED_LOW": 0,
        "servermain.TAG_SCALING_SCALED_HIGH": 1000,
        "servermain.TAG_SCALING_CLAMP_LOW": false,
        "servermain.TAG_SCALING_CLAMP_HIGH": false,
        "servermain.TAG_SCALING_NEGATE_VALUE": false
    }
    """;

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Data%20Type%20Examples/devices/16%20Bit%20Device/tag_groups/B%20Registers/tags/Boolean1")
                                   .ReturnsResponse(tagJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Tag>("Boolean1", new DeviceTagGroup("B Registers", new Device("16 Bit Device", new Channel("Data Type Examples"))));

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Boolean1", result.Name);
            Assert.Equal("Boolean register", result.Description);
            Assert.Equal("B0001", result.GetDynamicProperty<string>("servermain.TAG_ADDRESS"));
            Assert.Equal(1, result.GetDynamicProperty<int>("servermain.TAG_DATA_TYPE"));
            Assert.Equal(1, result.GetDynamicProperty<int>("servermain.TAG_READ_WRITE_ACCESS"));
            Assert.Equal(100, result.GetDynamicProperty<int>("servermain.TAG_SCAN_RATE_MILLISECONDS"));
            Assert.False(result.GetDynamicProperty<bool>("servermain.TAG_AUTOGENERATED"));
            Assert.Equal(0, result.GetDynamicProperty<int>("servermain.TAG_SCALING_TYPE"));
            Assert.Equal(0, result.GetDynamicProperty<int>("servermain.TAG_SCALING_RAW_LOW"));
            Assert.Equal(1000, result.GetDynamicProperty<int>("servermain.TAG_SCALING_RAW_HIGH"));
            Assert.Equal(9, result.GetDynamicProperty<int>("servermain.TAG_SCALING_SCALED_DATA_TYPE"));
            Assert.Equal(0, result.GetDynamicProperty<int>("servermain.TAG_SCALING_SCALED_LOW"));
            Assert.Equal(1000, result.GetDynamicProperty<int>("servermain.TAG_SCALING_SCALED_HIGH"));
            Assert.False(result.GetDynamicProperty<bool>("servermain.TAG_SCALING_CLAMP_LOW"));
            Assert.False(result.GetDynamicProperty<bool>("servermain.TAG_SCALING_CLAMP_HIGH"));
            Assert.False(result.GetDynamicProperty<bool>("servermain.TAG_SCALING_NEGATE_VALUE"));
        }

        #endregion

    }
}
