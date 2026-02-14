using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kepware.Api.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;
using Xunit;

namespace Kepware.Api.TestIntg.ApiClient
{
    [TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
    public class LoadEntityTests : TestIntgApiClientBase
    {
        #region LoadEntityAsync - Channel (Collection)

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnChannelCollection_WhenApiRespondsSuccessfully()
        {
            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<ChannelCollection, Channel>();

            // Assert
            Assert.NotNull(result);
            result.ShouldBeOfType<ChannelCollection>();

        }

        #endregion

        #region LoadEntityAsync - Single Channel

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnChannel_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var channel = await AddTestChannel();

            // Act

            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>(channel.Name);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(channel.Name, result.Name);
            Assert.Equal(channel.Description, result.Description);

            // Cleanup
            await DeleteAllChannelsAsync();
        }

        #endregion

        #region LoadEntityAsync - Device (Collection)

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnDeviceCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var channel = await AddTestChannel();

            var devicesList = new List<Device>();
            devicesList.Add(CreateTestDevice(channel, "TestDevice1"));
            devicesList.Add(CreateTestDevice(channel, "TestDevice2"));
            foreach (var device in devicesList)
            {
                await _kepwareApiClient.Project.Devices.GetOrCreateDeviceAsync(channel, device.Name);
            }

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceCollection, Device>([channel.Name]);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, d => d.Name == "TestDevice1");
            Assert.Contains(result, d => d.Name == "TestDevice2");

            // Cleanup
            await DeleteAllChannelsAsync();
        }

        #endregion

        #region LoadEntityAsync - Single Device

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnDevice_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Device>(device.Name, channel);

            // Assert
            Assert.NotNull(result);
            result.Name.ShouldNotBeNull();
            Assert.Equal(channel.Name, result.Owner?.Name);

            // Cleanup
            await DeleteAllChannelsAsync();

        }

        #endregion

        #region LoadEntityAsync - Error Handling

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnNull_WhenApiReturns404()
        {
            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>("InvalidChannel");

            // Assert
            Assert.Null(result);
        }

        #endregion

        // TODO: Add these types of test for other entities (Device, TagGroup, Tag, etc.)
        #region LoadEntityAsync - Device with DynamicProperties

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnDevice_WithCorrectDynamicProperties()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Device>(device.Name, channel);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(device.Name, result.Name);
            Assert.Equal(device.Description, result.Description);
            Assert.Equal(device.Owner!.Name, result.Owner?.Name);

            // Check if DynamicProperties are set correctly
            // Expected defaults based on device creation
            Assert.Equal(1, result.GetDynamicProperty<int>("servermain.DEVICE_ID_FORMAT"));
            Assert.Equal("1", result.GetDynamicProperty<string>("servermain.DEVICE_ID_STRING"));
            Assert.Equal(1, result.GetDynamicProperty<int>("servermain.DEVICE_ID_HEXADECIMAL"));
            Assert.Equal(1, result.GetDynamicProperty<int>("servermain.DEVICE_ID_DECIMAL"));
            Assert.Equal(1, result.GetDynamicProperty<int>("servermain.DEVICE_ID_OCTAL"));
            Assert.True(result.GetDynamicProperty<bool>("servermain.DEVICE_DATA_COLLECTION"));
            Assert.Equal(0, result.GetDynamicProperty<int>("servermain.DEVICE_SCAN_MODE"));
            Assert.Equal(1000, result.GetDynamicProperty<int>("servermain.DEVICE_SCAN_MODE_RATE_MS"));
            Assert.False(result.GetDynamicProperty<bool>("servermain.DEVICE_SCAN_MODE_PROVIDE_INITIAL_UPDATES_FROM_CACHE"));

            // Cleanup
            await DeleteAllChannelsAsync();
        }

        #endregion

        #region LoadEntityAsync - TagGroup (Collection)

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTagGroupCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);
            var tagGroup = await AddTestTagGroup(device);
            var tagGroup2 = await AddTestTagGroup(device, "TagGroup2");

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, g => g.Name == tagGroup.Name);
            Assert.Contains(result, g => g.Name == tagGroup2.Name);

            // Cleanup
            await DeleteAllChannelsAsync();
        }

        #endregion

        #region LoadEntityAsync - Single TagGroup

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTagGroup_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);
            var tagGroup = await AddTestTagGroup(device);

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<DeviceTagGroup>(tagGroup.Name, device);

            // Assert
            Assert.NotNull(result);

            // Cleanup
            await DeleteAllChannelsAsync();
        }

        #endregion

        #region LoadEntityAsync - Single Tag with DynamicProperties

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTag_WithCorrectDynamicProperties()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);
            var tag = await AddSimulatorTestTag(device);

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadEntityAsync<Tag>(tag.Name, device);

            // Assert
            // Expected defaults based on tag creation
            Assert.NotNull(result);
            Assert.Equal(tag.Name, result.Name);
            Assert.Equal(tag.Description, result.Description);
            Assert.Equal(tag.TagAddress, result.GetDynamicProperty<string>("servermain.TAG_ADDRESS"));
            Assert.Equal(5, result.GetDynamicProperty<int>("servermain.TAG_DATA_TYPE"));
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

            // Cleanup
            await DeleteAllChannelsAsync();
        }

        #endregion

        #region LoadEntityAsync - Tag (Collection)

        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTagCollection_WhenApiRespondsSuccessfully()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);
            var tagList = await AddSimulatorTestTags(device);

            // Act
            var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(device);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Name == tagList[0].Name);
            Assert.Contains(result, t => t.Name == tagList[1].Name);

            // Cleanup
            await DeleteAllChannelsAsync();
        }

        #endregion

        #region LoadEntityAsync - TagGroup Nested
        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTagGroupCollectionInTagGroup_WhenApiRespondsSuccessfully()
        {
            // TODO: Currently this test fails due to issue in EndpointResolver.
            // Arrange
            try
            {
                var channel = await AddTestChannel();
                var device = await AddTestDevice(channel);
                var tagGroup = await AddTestTagGroup(device);
                var tagGroup2 = await AddTestTagGroup(tagGroup, "TagGroup2");

                // Act
                var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(tagGroup);

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.Contains(result, g => g.Name == tagGroup2.Name);

            }
            catch (Exception ex)
            {
               Assert.Fail($"Currently this test fails due to issue in EndpointResolver. Test failed with exception: \n {ex}");
            }

            // Cleanup
            await DeleteAllChannelsAsync();
        }
        #endregion

        #region LoadEntityAsync - TagGroup Tag Collection
        [Fact]
        public async Task LoadEntityAsync_ShouldReturnTagCollectionFromTagGroup_WhenApiRespondsSuccessfully()
        {
            // TODO: Currently this test fails due to issue in EndpointResolver.
            try
            {
                // Arrange
                var channel = await AddTestChannel();
                var device = await AddTestDevice(channel);
                var tagGroup = await AddTestTagGroup(device);
                var tagList = await AddSimulatorTestTags(tagGroup);

                // Act
                var result = await _kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(tagGroup);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count);
                Assert.Contains(result, t => t.Name == tagList[0].Name);
                Assert.Contains(result, t => t.Name == tagList[1].Name);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Currently this test fails due to issue in EndpointResolver. Test failed with exception: \n {ex}");
            }

            // Cleanup
            await DeleteAllChannelsAsync();
        }
        #endregion
    }
}
