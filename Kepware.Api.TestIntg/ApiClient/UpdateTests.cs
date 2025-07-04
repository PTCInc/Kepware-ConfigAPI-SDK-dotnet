using Kepware.Api.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using System.Net;
using System.Text.Json;

namespace Kepware.Api.TestIntg.ApiClient;

public class UpdateTests : TestApiClientBase
{
    [Fact]
    public async Task Update_Item_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var channel = await AddTestChannel();
        channel.Description = "Updated Description";

        // Act
        var result = await _kepwareApiClient.GenericConfig.UpdateItemAsync(channel);

        // Assert
        result.ShouldBeTrue();

        // Clean up
        await DeleteAllChannelsAsync();
    }

    [Fact]
    public async Task Update_Item_WhenNotFound_ShouldReturnFalse()
    {
        // Arrange
        var channel = CreateTestChannel();

        // Act
        var result = await _kepwareApiClient.GenericConfig.UpdateItemAsync(channel);

        // Assert
        result.ShouldBeFalse();

        // Clean up
        await DeleteAllChannelsAsync();
    }

    [Fact]
    public async Task Update_MultipleItems_ShouldUpdateAll()
    {
        // Arrange
        var channel = await AddTestChannel();
        var device = await AddTestDevice(channel);
        var tagsAdded = await AddSimulatorTestTags(device);
        var tags = new List<(Tag item, Tag? oldItem)>();
        foreach (var tag in tagsAdded)
        {
            tags.Add((new Tag { Name = tag.Name, Description = "Updated Description" }, tag));
        };

        // Act
        var results = await _kepwareApiClient.GenericConfig.UpdateItemsAsync<DeviceTagCollection, Tag>(tags, device);

        // Assert
        results.Count.ShouldBe(tags.Count);
        results.ShouldAllBe(r => r == true);

        // Clean up
        await DeleteAllChannelsAsync();

    }

    [Fact]
    public async Task Update_Item_WithHttpError_ShouldReturnFalse()
    {
        // Arrange
        var channel = await AddTestChannel();
        channel.SetDynamicProperty("InvalidProperty", "InvalidValue");

        // Act
        var result = await _kepwareApiClient.GenericConfig.UpdateItemAsync(channel);

        // Assert
        result.ShouldBeFalse();

        // Clean up
        await DeleteAllChannelsAsync();
    }
}