using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace Kepware.Api.TestIntg.ApiClient;

public class InsertTests : TestIntgApiClientBase
{
    [Fact]
    public async Task Insert_Item_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var channel = CreateTestChannel();

        // Act
        var result = await _kepwareApiClient.GenericConfig.InsertItemAsync<ChannelCollection, Channel>(channel);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllChannelsAsync();
    }

    [Fact]
    public async Task Insert_Item_WithHttpError_ShouldReturnFalse()
    {
        // Arrange  
        var channel = CreateTestChannel();
        channel.SetDynamicProperty("InvalidProperty","InvalidValue");

        // Act  
        var result = await _kepwareApiClient.GenericConfig.InsertItemAsync<ChannelCollection, Channel>(channel);

        // Assert  
        result.ShouldBeFalse();

        // Clean up
        await DeleteAllChannelsAsync();
    }

    [Fact]
    public async Task Insert_MultipleItems_WhenSuccessful_ShouldReturnAllTrue()
    {
        // Arrange
        var channel = await AddTestChannel();
        var device = await AddTestDevice(channel);
        var tags = CreateSimulatorTestTags();

        // Act
        var results = await _kepwareApiClient.GenericConfig.InsertItemsAsync<DeviceTagCollection, Tag>(tags, owner: device);

        // Assert
        results.ShouldAllBe(r => r == true);

        // Clean up
        await DeleteAllChannelsAsync();
    }

    [Fact]
    public async Task Insert_MultipleItems_WithPartialSuccess_ShouldReturnMixedResults()
    {
        // Arrange
        var channel = await AddTestChannel();
        var device = await AddTestDevice(channel);
        var tags = CreateSimulatorTestTags();
        tags[1].TagAddress = "InvalidAddress";

        // Act
        var results = await _kepwareApiClient.GenericConfig.InsertItemsAsync<DeviceTagCollection, Tag>(tags, owner: device);

        // Assert
        results.Length.ShouldBe(2);
        results[0].ShouldBeTrue();
        results[1].ShouldBeFalse();

        // Clean up
        await DeleteAllChannelsAsync();
    }

    [Fact]
    public async Task Insert_MultipleItems_WithUnsupportedDriver_ShouldSkipItems()
    {
        // Arrange
        var channel1 = CreateTestChannel("Channel1", "UnsupportedDriver");
        var channel2 = CreateTestChannel("Channel2", "Simulator");
        var channels = new List<Channel> { channel1, channel2 };

        // Act
        var results = await _kepwareApiClient.GenericConfig.InsertItemsAsync<ChannelCollection, Channel>(channels);

        // Assert
        results.Length.ShouldBe(1); // Only the Simulator channel should be inserted  
        results[0].ShouldBeTrue();

        // Clean up
        await DeleteAllChannelsAsync();

    }
}