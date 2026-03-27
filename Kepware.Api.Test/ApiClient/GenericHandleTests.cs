using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Kepware.Api.Model;
using Kepware.Api.Test.ApiClient;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using Xunit;

namespace Kepware.Api.Test.ApiClient
{
    public class GenericHandler : TestApiClientBase
    {
                [Fact]
                public async Task CompareAndApplyDetailed_ShouldCountUpdateAsFailed_When200ContainsNotApplied()
                {
                        // Arrange
                        var channel = new Channel { Name = "Channel1" };
                        channel.SetDynamicProperty(Properties.Channel.DeviceDriver, "Simulator");

                        var sourceDevice = new Device { Name = "Device1", Description = "new description", Owner = channel };
                        var targetDevice = new Device { Name = "Device1", Description = "old description", Owner = channel };

                        var sourceCollection = new DeviceCollection { sourceDevice };
                        var targetCollection = new DeviceCollection { targetDevice };

                        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Channel1/devices/Device1")
                                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(targetDevice), "application/json");

                        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, TEST_ENDPOINT + "/config/v1/project/channels/Channel1/devices/Device1")
                                .ReturnsResponse(HttpStatusCode.OK,
                                        """
                                        {
                                            "not_applied": {
                                                "servermain.DEVICE_ID_OCTAL": 1,
                                                "servermain.DEVICE_MODEL": 0
                                            },
                                            "code": 200,
                                            "message": "Not all properties were applied."
                                        }
                                        """,
                                        "application/json");

                        // Act
                        var result = await _kepwareApiClient.GenericConfig.CompareAndApplyDetailed<DeviceCollection, Device>(sourceCollection, targetCollection, channel);

                        // Assert
                        result.Updates.ShouldBe(0);
                        result.Failed.ShouldBe(1);
                        result.Failures.Count.ShouldBe(1);
                        result.Failures[0].Operation.ShouldBe(ApplyOperation.Update);
                        (result.Failures[0].AttemptedItem as Device)?.Name.ShouldBe("Device1");
                        result.Failures[0].NotAppliedProperties.ShouldNotBeNull();
                        result.Failures[0].NotAppliedProperties!.ShouldContain("servermain.DEVICE_ID_OCTAL");
                        result.Failures[0].NotAppliedProperties!.ShouldContain("servermain.DEVICE_MODEL");
                }

                [Fact]
                public async Task CompareAndApplyDetailed_ShouldMap207InsertFeedbackToItems()
                {
                        // Arrange
                        var channel = new Channel { Name = "Channel1" };
                        channel.SetDynamicProperty(Properties.Channel.DeviceDriver, "Simulator");
                        var ownerDevice = new Device { Name = "Device1", Owner = channel };

                        var tag1 = new Tag { Name = "Tag1", TagAddress = "RAMP" };
                        var tag2 = new Tag { Name = "Tag2", TagAddress = "SINE" };

                        var sourceTags = new DeviceTagCollection { tag1, tag2 };

                        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, TEST_ENDPOINT + "/config/v1/project/channels/Channel1/devices/Device1/tags")
                                .ReturnsResponse((HttpStatusCode)207,
                                        """
                                        [
                                            {
                                                "property": "common.ALLTYPES_NAME",
                                                "description": "The name 'Tag1' is already used.",
                                                "error_line": 3,
                                                "code": 400,
                                                "message": "Validation failed on property common.ALLTYPES_NAME in object definition at line 3: The name 'Tag1' is already used."
                                            },
                                            {
                                                "code": 201,
                                                "message": "Created"
                                            }
                                        ]
                                        """,
                                        "application/json");

                        // Act
                        var result = await _kepwareApiClient.GenericConfig.CompareAndApplyDetailed<DeviceTagCollection, Tag>(sourceTags, null, ownerDevice);

                        // Assert
                        result.Inserts.ShouldBe(1);
                        result.Failed.ShouldBe(1);
                        result.Failures.Count.ShouldBe(1);
                        result.Failures[0].Operation.ShouldBe(ApplyOperation.Insert);
                        (result.Failures[0].AttemptedItem as Tag)?.Name.ShouldBe("Tag1");
                        result.Failures[0].ResponseCode.ShouldBe(400);
                        result.Failures[0].Property.ShouldBe("common.ALLTYPES_NAME");
                        result.Failures[0].Description.ShouldBe("The name 'Tag1' is already used.");
                        result.Failures[0].ErrorLine.ShouldBe(3);
                }

        [Fact]
        public void AppendQueryString_PrivateMethod_EncodesAndSkipsNullsAndAppendsCorrectly()
        {
            // Arrange
            var method = typeof(Kepware.Api.ClientHandler.GenericApiHandler)
                .GetMethod("AppendQueryString", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var query = new[]
            {
                new KeyValuePair<string, string?>("a", "b"),
                new KeyValuePair<string, string?>("space", "x y"),
                new KeyValuePair<string, string?>("skip", null) // should be skipped
            };

            // Act
            var result1 = (string)method!.Invoke(null, new object[] { "https://api/config", query })!;
            var result2 = (string)method!.Invoke(null, new object[] { "https://api/config?existing=1", query })!;

            // Assert
            Assert.Equal("https://api/config?a=b&space=x%20y", result1);
            Assert.Equal("https://api/config?existing=1&a=b&space=x%20y", result2);
        }

        [Fact]
        public async Task LoadCollectionAsync_AppendsQueryAndReturnsCollection()
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

            var query = new[]
            {
                new KeyValuePair<string, string?>("status", "active"),
                new KeyValuePair<string, string?>("name", "John Doe"),
                new KeyValuePair<string, string?>("skip", null)
            };

            // Expect encoded space in "John Doe" and null entry skipped
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels?status=active&name=John%20Doe")
                                   .ReturnsResponse(channelsJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<ChannelCollection, Channel>((string?)null, query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.Name == "Channel1");
            Assert.Contains(result, c => c.Name == "Data Type Examples");
        }

        [Fact]
        public async Task LoadEntityAsync_AppendsQueryAndReturnsEntity()
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

            var query = new[]
            {
                new KeyValuePair<string, string?>("status", "active"),
                new KeyValuePair<string, string?>("name", "John Doe"),
                new KeyValuePair<string, string?>("skip", null)
            };

            // Expect encoded space in "John Doe" and null entry skipped
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels/Channel1?status=active&name=John%20Doe")
                                   .ReturnsResponse(channelJson, "application/json");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>("Channel1", query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Channel1", result.Name);
            Assert.Equal("Example Simulator Channel", result.Description);
        }
    }
}
