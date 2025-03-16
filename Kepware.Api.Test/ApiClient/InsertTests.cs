using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace Kepware.Api.Test.ApiClient;

public class InsertTests : TestApiClientBase
{
    [Fact]
    public async Task Insert_Item_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        await ConfigureToServeDrivers();
        var channel = CreateTestChannel();
        var endpoint = "/config/v1/project/channels";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var result = await _kepwareApiClient.GenericConfig.InsertItemAsync<ChannelCollection, Channel>(channel);

        // Assert
        result.ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task Insert_Item_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        await ConfigureToServeDrivers();
        var channel = CreateTestChannel();
        var endpoint = "/config/v1/project/channels";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _kepwareApiClient.GenericConfig.InsertItemAsync<ChannelCollection, Channel>(channel);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
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
    public async Task Insert_Item_WithConnectionError_ShouldHandleGracefully()
    {
        // Arrange
        await ConfigureToServeDrivers();
        var channel = CreateTestChannel();
        var endpoint = "/config/v1/project/channels";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .Throws(new HttpRequestException("Connection error"));

        // Act
        var result = await _kepwareApiClient.GenericConfig.InsertItemAsync<ChannelCollection, Channel>(channel);

        // Assert
        result.ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
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
    public async Task Insert_MultipleItems_WhenSuccessful_ShouldReturnAllTrue()
    {
        // Arrange
        await ConfigureToServeDrivers();
        var channel = CreateTestChannel();
        var device = CreateTestDevice(channel);
        var tags = CreateTestTags();
        var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var results = await _kepwareApiClient.GenericConfig.InsertItemsAsync<DeviceTagCollection, Tag>(tags, owner: device);

        // Assert
        results.ShouldAllBe(r => r == true);
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
    }

    [Fact]
    public async Task Insert_MultipleItems_WithPartialSuccess_ShouldReturnMixedResults()
    {
        // Arrange
        await ConfigureToServeDrivers();
        var channel = CreateTestChannel();
        var device = CreateTestDevice(channel);
        var tags = CreateTestTags();
        var endpoint = $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}/tags";

        var multiStatusResponse = JsonSerializer.Serialize(new List<ApiResult>
        {
            new() { Code = 201, Message = "Created" },
            new() { Code = 400, Message = "Bad Request" }
        }, KepJsonContext.Default.ListApiResult);

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.MultiStatus, multiStatusResponse, "application/json");

        // Act
        var results = await _kepwareApiClient.GenericConfig.InsertItemsAsync<DeviceTagCollection, Tag>(tags, owner: device);

        // Assert
        results.Length.ShouldBe(2);
        results[0].ShouldBeTrue();
        results[1].ShouldBeFalse();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
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
    public async Task Insert_MultipleItems_WithUnsupportedDriver_ShouldSkipItems()
    {
        // Arrange
        await ConfigureToServeDrivers();
        var channel1 = CreateTestChannel("Channel1", "UnsupportedDriver");
        var channel2 = CreateTestChannel("Channel2", "Advanced Simulator");
        var channels = new List<Channel> { channel1, channel2 };
        var endpoint = "/config/v1/project/channels";

        _httpMessageHandlerMock.SetupRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}")
            .ReturnsResponse(HttpStatusCode.OK);

        // Act
        var results = await _kepwareApiClient.GenericConfig.InsertItemsAsync<ChannelCollection, Channel>(channels);

        // Assert
        results.Length.ShouldBe(1); // Nur der Advanced Simulator-Channel sollte eingefügt werden
        results[0].ShouldBeTrue();
        _httpMessageHandlerMock.VerifyRequest(HttpMethod.Post, $"{TEST_ENDPOINT}{endpoint}", Times.Once());
        _loggerMockGeneric.Verify(logger => 
            logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
            Times.Once);
    }
} 