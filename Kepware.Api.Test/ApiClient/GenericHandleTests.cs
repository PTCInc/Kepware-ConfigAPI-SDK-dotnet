using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Kepware.Api.Model;
using Kepware.Api.Test.ApiClient;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Xunit;

namespace Kepware.Api.Test.ApiClient
{
    public class GenericHandler : TestApiClientBase
    {
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
