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
using Shouldly;
using System.Text.Json;

namespace Kepware.Api.TestIntg.ApiClient
{
    public class ProjectApiHandlerTests : TestIntgApiClientBase
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
            var result = await _projectApiHandler.Channels.GetOrCreateChannelAsync(channel.Name, channel.DeviceDriver!);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(channel.Name, result.Name);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        [Fact]
        public async Task GetChannelAsync_ShouldReturnChannel_WhenChannelExists()
        {
            // Arrange
            var channel = await AddTestChannel();

            // Act
            var result = await _projectApiHandler.Channels.GetChannelAsync(channel.Name);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(channel.Name, result.Name);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        [Fact]
        public async Task GetChannelAsync_ShouldReturnNull_WhenChannelDoesNotExist()
        {
            // Arrange
            var channel = CreateTestChannel();

            // Act
            var result = await _projectApiHandler.Channels.GetChannelAsync(channel.Name);

            // Assert
            Assert.Null(result);

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
        public async Task GetDeviceAsync_ShouldReturnDevice_WhenDeviceExists()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = await AddTestDevice(channel);

            // Act
            var result = await _projectApiHandler.Devices.GetDeviceAsync(channel, device.Name);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(device.Name, result.Name);

            // Clean up
            await DeleteAllChannelsAsync();
        }

        [Fact]
        public async Task GetDeviceAsync_ShouldReturnNull_WhenDeviceDoesNotExist()
        {
            // Arrange
            var channel = await AddTestChannel();
            var device = CreateTestDevice(channel);

            // Act
            var result = await _projectApiHandler.Devices.GetDeviceAsync(channel, device.Name);

            // Assert
            Assert.Null(result);

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
        #region CompareAndApply Tests

        // [Fact]
        // public async Task CompareAndApply_ShouldReturnNull_WhenHttpRequestFails()
        // {
        //     // Arrange
        //     var newProject = new Project { Description = "Test Project" };

        //     // Act
        //     var result = await _badCredKepwareApiClient.Project.CompareAndApply(newProject);

        //     // Assert
        //     result.ShouldBeNull();
        // }

        [Fact]
        public async Task CompareAndApply_ShouldReturnChanges()
        {
            // Arrange
            var channel = CreateTestChannel("ExistingChannel1", "Simulator");
            var r = await _kepwareApiClient.Project.Channels.GetOrCreateChannelAsync(channel.Name, channel.DeviceDriver!);
            r.ShouldNotBeNull();

            var existingProperties = await _kepwareApiClient.Project.GetProjectPropertiesAsync();
            existingProperties.ShouldNotBeNull();

            // Create source project with 2 new channels and modified properties
            var newProject = new Project
            {
                Description = "Modified Project Description",
                Channels = new ChannelCollection
                {
                    new Channel { Name = "NewChannel1", DynamicProperties = new Dictionary<string, JsonElement> { { "servermain.CHANNEL_UNIQUE_ID", JsonDocument.Parse($"592352578").RootElement }, {"servermain.MULTIPLE_TYPES_DEVICE_DRIVER", JsonDocument.Parse($"\"Simulator\"").RootElement} } },
                    new Channel { Name = "NewChannel2", DynamicProperties = new Dictionary<string, JsonElement> { { "servermain.CHANNEL_UNIQUE_ID", JsonDocument.Parse($"592352577").RootElement }, {"servermain.MULTIPLE_TYPES_DEVICE_DRIVER", JsonDocument.Parse($"\"Simulator\"").RootElement} } }
                }
            };
            newProject.SetDynamicProperty("uaserverinterface.PROJECT_OPC_UA_ANONYMOUS_LOGIN", !existingProperties?.ProjectProperties.OpcUaAllowAnonymousLogin);

            // Act
            var result = await _kepwareApiClient.Project.CompareAndApply(newProject);

            // Assert
            Assert.Equal(2, result.inserts);  // 2 new channels added
            Assert.Equal(1, result.updates);  // 1 project property updates
            Assert.Equal(1, result.deletes);  // 1 existing channel deleted

            // Clean up
            await DeleteAllChannelsAsync();
        }

        #endregion
    }
}

