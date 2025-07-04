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

namespace Kepware.Api.TestIntg.ApiClient
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
            var channel = await AddTestChannel();

            // Act
            var result = await _projectApiHandler.Channels.GetOrCreateChannelAsync(channel.Name, channel.DeviceDriver!);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(channel.Name, result.Name);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        [Fact]
        public async Task GetOrCreateChannelAsync_ShouldCreateChannel_WhenChannelDoesNotExist()
        {
            // Arrange
            var channel = CreateTestChannel();

            // Act
            var result = await _projectApiHandler.Channels.GetOrCreateChannelAsync(channel.Name, channel.DeviceDriver);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(channel.Name, result.Name);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        [Fact]
        public async Task UpdateChannelAsync_ShouldReturnTrue_WhenUpdateIsSuccessful()
        {
            // Arrange
            var channel = await AddTestChannel();
            channel.Description = "Updated Description";

            // Act
            var result = await _projectApiHandler.Channels.UpdateChannelAsync(channel);

            // Assert
            Assert.True(result);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        [Fact]
        public async Task DeleteChannelAsync_ShouldReturnTrue_WhenDeletionIsSuccessful()
        {
            // Arrange
            var channel = await AddTestChannel();

            // Act
            var result = await _projectApiHandler.Channels.DeleteChannelAsync(channel);

            // Assert
            Assert.True(result);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        #endregion

        #region Device Tests

        [Fact]
        public async Task GetOrCreateDeviceAsync_ShouldReturnDevice_WhenDeviceExists()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);

            // Act
            var result = await _projectApiHandler.Devices.GetOrCreateDeviceAsync(channel, device.Name);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(device.Name, result.Name);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        [Fact]
        public async Task GetOrCreateDeviceAsync_ShouldCreateDevice_WhenDeviceDoesNotExist()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = CreateTestDevice(channel);

            // Act
            var result = await _projectApiHandler.Devices.GetOrCreateDeviceAsync(channel, device.Name);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(device.Name, result.Name);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        [Fact]
        public async Task UpdateDeviceAsync_ShouldReturnTrue_WhenUpdateIsSuccessful()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);
            device.Description = "Updated Device Description";

            // Act
            var result = await _projectApiHandler.Devices.UpdateDeviceAsync(device);

            // Assert
            Assert.True(result);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        [Fact]
        public async Task DeleteDeviceAsync_ShouldReturnTrue_WhenDeletionIsSuccessful()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);

            // Act
            var result = await _projectApiHandler.Devices.DeleteDeviceAsync(device);

            // Assert
            Assert.True(result);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        #endregion

        #region Device Tag & Tag Group Tests

        [Fact]
        public async Task LoadTagGroupsRecursiveAsync_ShouldLoadTagGroupsCorrectly()
        {
            // TODO: Currently this test fails due to isse in EndpointResolver.
            try
            {
                // Arrange
                var channel = await AddTestChannel();
                var device = await AddTestDevice(channel);
                var tagGroup = await AddTestTagGroup(device);
                var tagGroup2 = await AddTestTagGroup(device, "TagGroup2");
                var tagGroup3 = await AddTestTagGroup(tagGroup);

                var tagGroups = new List<DeviceTagGroup> { tagGroup, tagGroup2, tagGroup3 };

                // Act
                await ProjectApiHandler.LoadTagGroupsRecursiveAsync(_kepwareApiClient, tagGroups);

                // Assert
                Assert.NotNull(tagGroup.TagGroups);
                Assert.Single(tagGroup.TagGroups);
                Assert.Equal("TagGroup1", tagGroup.TagGroups.First().Name);
                Assert.NotNull(tagGroup2.TagGroups);
                Assert.Empty(tagGroup2.TagGroups);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Currently this test fails due to issue in EndpointResolver. Test failed with exception: \n {ex}");
            }

            // Clean up
            await DeleteAllChannelsAsync();
        }

        #endregion
    }
}

