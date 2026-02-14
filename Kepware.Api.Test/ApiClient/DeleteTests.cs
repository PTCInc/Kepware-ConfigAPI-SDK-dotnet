using Kepware.Api.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;

namespace Kepware.Api.Test.ApiClient;

public class DeleteTests : TestApiClientBase
{
    [Fact]
    public async Task Delete_ItemByNamedEntity_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync(channel);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task Delete_ItemByNamedEntity_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync(channel);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
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
    public async Task Delete_ItemByNamedEntity_WithConnectionError_ShouldReturnFalse()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .Throws(new HttpRequestException("Connection error"));

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync(channel);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _loggerMockGeneric.Verify(logger =>
            logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<HttpRequestException>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Delete_ItemByName_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync<Channel>("TestChannel");

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task Delete_ItemByName_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync<Channel>("TestChannel");

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
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
    public async Task Delete_ItemByMultipleNames_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var itemNames = new[] { "Channel1", "Device1" };
        var endpoint = "/config/v1/project/channels/Channel1/devices/Device1";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync<Device>(itemNames);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task Delete_ItemByMultipleNames_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var itemNames = new[] { "Channel1", "Device1" };
        var endpoint = "/config/v1/project/channels/Channel1/devices/Device1";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync<Device>(itemNames);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
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
    public async Task Delete_Item_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync<ChannelCollection, Channel>(channel);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task Delete_Item_WithHttpError_ShouldLogError()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync<ChannelCollection, Channel>(channel);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
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
    public async Task Delete_Item_WithConnectionError_ShouldHandleGracefully()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var endpoint = "/config/v1/project/channels/TestChannel";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .Throws(new HttpRequestException("Connection error"));

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync<ChannelCollection, Channel>(channel);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _loggerMockGeneric.Verify(logger =>
            logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<HttpRequestException>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Delete_Item_WithOwner_WhenSuccessful_ShouldDeleteAll()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var device = new Device { Name = "ParentDevice", Owner = channel };
        var tag = new Tag { Name = "Tag1", Owner = device };
        var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags/{tag.Name}";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync<DeviceTagCollection, Tag>(tag, owner: device);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task Delete_Item_WithOwner_WithHttpError_ShouldLogError()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var device = new Device { Name = "ParentDevice", Owner = channel };
        var tag = new Tag { Name = "Tag1", Owner = device };
        var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags/{tag.Name}";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.NotFound, "Not Found");

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemAsync<DeviceTagCollection, Tag>(tag, owner: device);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
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
    public async Task Delete_MultipleItems_WhenSuccessful_ShouldDeleteAll()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var device = new Device { Name = "ParentDevice", Owner = channel };
        var tags = CreateTestTags();

        foreach (var tag in tags)
        {
            var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags/{tag.Name}";
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
                .ReturnsResponse(HttpStatusCode.OK);
        }

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemsAsync<DeviceTagCollection, Tag>(tags, owner: device);

        // Assert
        result.ShouldBeTrue();
        foreach (var tag in tags)
        {
            var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags/{tag.Name}";
            _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        }
    }

    [Fact]
    public async Task Delete_MultipleItems_WithHttpError_ShouldLogError()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var device = new Device { Name = "ParentDevice", Owner = channel };
        var tags = CreateTestTags(1);
        var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags/{tags[0].Name}";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemsAsync<DeviceTagCollection, Tag>(tags, owner: device);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
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
    public async Task Delete_MultipleItems_WithConnectionError_ShouldHandleGracefully()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var device = new Device { Name = "ParentDevice", Owner = channel };
        var tags = CreateTestTags(1);
        var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags/{tags[0].Name}";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}")
            .Throws(new HttpRequestException("Connection error"));

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemsAsync<DeviceTagCollection, Tag>(tags, owner: device);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _loggerMockGeneric.Verify(logger =>
            logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<HttpRequestException>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Delete_MultipleItems_WithEmptyList_ShouldNoop()
    {
        // Arrange
        var channel = new Channel { Name = "TestChannel" };
        var device = new Device { Name = "ParentDevice", Owner = channel };
        var tags = new List<Tag>();
        var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags/Tag1";

        // Act
        var result = await _kepwareApiClient.GenericConfig.DeleteItemsAsync<DeviceTagCollection, Tag>(tags, owner: device);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Delete, $"{TEST_ENDPOINT}{endpoint}", Times.Never());
    }
}
