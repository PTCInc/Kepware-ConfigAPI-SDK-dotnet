using Kepware.Api.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace Kepware.Api.Test.ApiClient;

public class UpdateTests : TestApiClientBase
{
    [Fact]
    public async Task Update_Item_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var existingChannel = new Channel { Name = "TestChannel", ProjectId = 1234 };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(existingChannel));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.GenericConfig.UpdateItemAsync(channel);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task Update_Item_WhenNotFound_ShouldReturnFalse()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.NotFound);

        // Act
        var result = await _kepwareApiClient.GenericConfig.UpdateItemAsync(channel);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{endpoint}", Times.Never());
    }

    [Fact]
    public async Task Update_MultipleItems_ShouldUpdateAll()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var device = new Device { Name = "ParentDevice", Owner = channel };
        var tags = new List<(Tag item, Tag? oldItem)>
        {
            (new Tag { Name = "Tag1", Description = "Updated Description" }, new Tag { Name = "Tag1" }),
            (new Tag { Name = "Tag2", Description = "Updated Description" }, new Tag { Name = "Tag2" })
        };

        foreach (var tag in tags)
        {
            var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags/{tag.oldItem!.Name}";
            var existingTag = new Tag { Name = tag.oldItem.Name, ProjectId = 1234 };
            
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
                .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(existingTag));

            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{endpoint}")
                .ReturnsResponse(HttpStatusCode.OK);
        }

        // Act
        await _kepwareApiClient.GenericConfig.UpdateItemsAsync<DeviceTagCollection, Tag>(tags, device);

        // Assert
        foreach (var tag in tags)
        {
            var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags/{tag.oldItem!.Name}";
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        }
    }

    [Fact]
    public async Task Update_Item_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var existingChannel = new Channel { Name = "TestChannel", ProjectId = 1234 };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(existingChannel));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.GenericConfig.UpdateItemAsync(channel);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _loggerMockGeneric.Verify(logger => 
            logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
            Times.Once);
    }

    [Fact]
    public async Task Update_Item_WithConnectionError_ShouldHandleGracefully()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var existingChannel = new Channel { Name = "TestChannel", ProjectId = 1234 };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(existingChannel));

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{endpoint}")
            .Throws(new HttpRequestException("Connection error"));

        // Act
        var result = await _kepwareApiClient.GenericConfig.UpdateItemAsync(channel);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Put, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _loggerMockGeneric.Verify(logger => 
            logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<HttpRequestException>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
            Times.Once);
    }
} 