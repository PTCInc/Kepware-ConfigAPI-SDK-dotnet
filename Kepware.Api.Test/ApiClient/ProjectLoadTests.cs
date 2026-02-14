using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Moq.Contrib.HttpClient;
using Microsoft.Extensions.Logging;
using Kepware.Api.Model;
using System.Linq;
using System.Collections.Generic;
using Kepware.Api.Serializer;
using Kepware.Api.Test.ApiClient;
using Kepware.Api.Util;
using Shouldly;

namespace Kepware.Api.Test.ApiClient
{
    public class ProjectLoadTests : TestApiClientBase
    {

        //private async Task ConfigureToServeEndpoints()
        //{
        //    var projectData = await LoadJsonTestDataAsync();

        //    var channels = projectData.Project?.Channels?.Select(c => new Channel { Name = c.Name, Description = c.Description, DynamicProperties = c.DynamicProperties }).ToList() ?? [];

        //    // Serve project details
        //    _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project")
        //                           .ReturnsResponse(JsonSerializer.Serialize(new Project { Description = projectData?.Project?.Description, DynamicProperties = projectData?.Project?.DynamicProperties ?? [] }), "application/json");

        //    // Serve channels without nested devices
        //    _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project/channels")
        //                           .ReturnsResponse(JsonSerializer.Serialize(channels), "application/json");

        //    foreach (var channel in projectData?.Project?.Channels ?? [])
        //    {
        //        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}")
        //                               .ReturnsResponse(JsonSerializer.Serialize(new Channel { Name = channel.Name, Description = channel.Description, DynamicProperties = channel.DynamicProperties }), "application/json");

        //        if (channel.Devices != null)
        //        {
        //            var devices = channel.Devices.Select(d => new Device { Name = d.Name, Description = d.Description, DynamicProperties = d.DynamicProperties }).ToList();
        //            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices")
        //                                   .ReturnsResponse(JsonSerializer.Serialize(devices), "application/json");

        //            foreach (var device in channel.Devices)
        //            {
        //                var deviceEndpoint = TEST_ENDPOINT + $"/config/v1/project/channels/{channel.Name}/devices/{device.Name}";
        //                _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, deviceEndpoint)
        //                                       .ReturnsResponse(JsonSerializer.Serialize(new Device { Name = device.Name, Description = device.Description, DynamicProperties = device.DynamicProperties }), "application/json");


        //                _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, deviceEndpoint + "/tags")
        //                                      .ReturnsResponse(JsonSerializer.Serialize(device.Tags), "application/json");

        //                ConfigureToServeEndpointsTagGroupsRecursive(deviceEndpoint, device.TagGroups ?? []);
        //            }
        //        }
        //    }
        //}

        //private void ConfigureToServeEndpointsTagGroupsRecursive(string endpoint, IEnumerable<DeviceTagGroup> tagGroups)
        //{
        //    var tagGroupEndpoint = endpoint + "/tag_groups";

        //    _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, tagGroupEndpoint)
        //                                     .ReturnsResponse(JsonSerializer.Serialize(tagGroups), "application/json");

        //    foreach (var tagGroup in tagGroups)
        //    {
        //        _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, string.Concat(tagGroupEndpoint, "/", tagGroup.Name, "/tags"))
        //                                     .ReturnsResponse(JsonSerializer.Serialize(tagGroup.Tags), "application/json");

        //        ConfigureToServeEndpointsTagGroupsRecursive(string.Concat(tagGroupEndpoint, "/", tagGroup.Name), tagGroup.TagGroups ?? []);
        //    }
        //}

        [Theory]
        [InlineData("KEPServerEX", "12", 6, 17, true)]
        [InlineData("KEPServerEX", "12", 6, 16, false)]
        [InlineData("ThingWorxKepwareEdge", "13", 1, 10, true)]
        [InlineData("ThingWorxKepwareEdge", "13", 1, 9, false)]
        [InlineData("UnknownProduct", "99", 10, 0, false)]
        public async Task LoadProject_ShouldLoadCorrectly_BasedOnProductSupport(
            string productName, string productId, int majorVersion, int minorVersion, bool supportsJsonLoad)
        {
            ConfigureConnectedClient(productName, productId, majorVersion, minorVersion);

            if (supportsJsonLoad)
            {
                await ConfigureToServeFullProject();
            }
            else
            {
                await ConfigureToServeEndpoints();
            }

            var project = await _kepwareApiClient.Project.LoadProject(true);

            project.IsLoadedByProjectLoadService.ShouldBe(supportsJsonLoad);

            project.ShouldNotBeNull();
            project.Channels.ShouldNotBeEmpty("Channels list should not be empty.");

            var testProject = await LoadJsonTestDataAsync();
            var compareResult = EntityCompare.Compare<ChannelCollection, Channel>(testProject?.Project?.Channels, project?.Channels);

            compareResult.ShouldNotBeNull();
            compareResult.UnchangedItems.ShouldNotBeEmpty("All channels should be unchanged.");
            compareResult.ChangedItems.ShouldBeEmpty("No channels should be changed.");
            compareResult.ItemsOnlyInLeft.ShouldBeEmpty("No channels should exist only in the test data.");
            compareResult.ItemsOnlyInRight.ShouldBeEmpty("No channels should exist only in the loaded project.");

            foreach (var (ExpectedChannel, LoadedChannel) in testProject?.Project?.Channels?.Zip(project?.Channels ?? []) ?? [])
            {
                var deviceCompareResult = EntityCompare.Compare<DeviceCollection, Device>(ExpectedChannel.Devices, LoadedChannel.Devices);
                deviceCompareResult.ShouldNotBeNull();
                deviceCompareResult.UnchangedItems.ShouldNotBeEmpty($"All devices in channel {ExpectedChannel.Name} should be unchanged.");
                deviceCompareResult.ChangedItems.ShouldBeEmpty($"No devices in channel {ExpectedChannel.Name} should be changed.");
                deviceCompareResult.ItemsOnlyInLeft.ShouldBeEmpty($"No devices should exist only in the test data for channel {ExpectedChannel.Name}.");
                deviceCompareResult.ItemsOnlyInRight.ShouldBeEmpty($"No devices should exist only in the loaded project for channel {ExpectedChannel.Name}.");

                foreach (var (ExpectedDevice, LoadedDevice) in ExpectedChannel.Devices?.Zip(LoadedChannel.Devices ?? []) ?? [])
                {
                    if (ExpectedDevice.Tags?.Count > 0 || LoadedDevice.Tags?.Count > 0)
                    {
                        var tagCompareResult = EntityCompare.Compare<DeviceTagCollection, Tag>(ExpectedDevice.Tags, LoadedDevice.Tags);
                        tagCompareResult.ShouldNotBeNull();
                        tagCompareResult.UnchangedItems.ShouldNotBeEmpty($"All tags in device {ExpectedDevice.Name} should be unchanged.");
                        tagCompareResult.ChangedItems.ShouldBeEmpty($"No tags in device {ExpectedDevice.Name} should be changed.");
                        tagCompareResult.ItemsOnlyInLeft.ShouldBeEmpty($"No tags should exist only in the test data for device {ExpectedDevice.Name}.");
                        tagCompareResult.ItemsOnlyInRight.ShouldBeEmpty($"No tags should exist only in the loaded project for device {ExpectedDevice.Name}.");
                    }

                    CompareTagGroupsRecursive(ExpectedDevice.TagGroups, LoadedDevice.TagGroups, ExpectedDevice.Name);
                }
            }
        }

        private static void CompareTagGroupsRecursive(DeviceTagGroupCollection? expected, DeviceTagGroupCollection? actual, string parentName)
        {
            if ((expected?.Count ?? 0) == 0 && (actual?.Count ?? 0) == 0)
                return;
            var tagGroupCompareResult = EntityCompare.Compare<DeviceTagGroupCollection, DeviceTagGroup>(expected, actual);

            tagGroupCompareResult.ShouldNotBeNull();
            tagGroupCompareResult.UnchangedItems.ShouldNotBeEmpty($"All tag groups in {parentName} should be unchanged.");
            tagGroupCompareResult.ChangedItems.ShouldBeEmpty($"No tag groups in {parentName} should be changed.");
            tagGroupCompareResult.ItemsOnlyInLeft.ShouldBeEmpty($"No tag groups should exist only in the test data for {parentName}.");
            tagGroupCompareResult.ItemsOnlyInRight.ShouldBeEmpty($"No tag groups should exist only in the loaded project for {parentName}.");

            foreach (var (ExpectedTagGroup, ActualTagGroup) in expected?.Zip(actual ?? []) ?? [])
            {
                var thisName = parentName + "/" + ExpectedTagGroup.Name;
                if (ExpectedTagGroup.Tags?.Count > 0 || ActualTagGroup.Tags?.Count > 0)
                {
                    var tagCompareResult = EntityCompare.Compare<DeviceTagCollection, Tag>(ExpectedTagGroup.Tags, ExpectedTagGroup.Tags);
                    tagCompareResult.ShouldNotBeNull();
                    tagCompareResult.UnchangedItems.ShouldNotBeEmpty($"All tags in device {thisName} should be unchanged.");
                    tagCompareResult.ChangedItems.ShouldBeEmpty($"No tags in device {thisName} should be changed.");
                    tagCompareResult.ItemsOnlyInLeft.ShouldBeEmpty($"No tags should exist only in the test data for device {thisName}.");
                    tagCompareResult.ItemsOnlyInRight.ShouldBeEmpty($"No tags should exist only in the loaded project for device {thisName}.");
                }

                CompareTagGroupsRecursive(ExpectedTagGroup.TagGroups, ActualTagGroup.TagGroups, thisName);
            }
        }

        [Theory]
        [InlineData("KEPServerEX", "12", 6, 17)]
        [InlineData("KEPServerEX", "12", 6, 16)]
        [InlineData("ThingWorxKepwareEdge", "13", 1, 10)]
        [InlineData("ThingWorxKepwareEdge", "13", 1, 9)]
        [InlineData("UnknownProduct", "99", 10, 0)]
        public async Task LoadProject_NotFull_ShouldLoadCorrectly_BasedOnProductSupport(
          string productName, string productId, int majorVersion, int minorVersion)
        {
            ConfigureConnectedClient(productName, productId, majorVersion, minorVersion);

            await ConfigureToServeEndpoints();

            var project = await _kepwareApiClient.Project.LoadProject(blnLoadFullProject: false);

            project.ShouldNotBeNull();
            project.Channels.ShouldBeNull("Channels list should be null.");

            foreach (var channel in project.Channels ?? [])
            {
                channel.Devices.ShouldBeNull("Devices should not be loaded when not requested.");
            }
        }

        [Fact]
        public async Task LoadProject_ShouldReturnEmptyProject_WhenHttpRequestFails()
        {
            // Arrange
            _httpMessageHandlerMock.SetupAnyRequest()
                                   .ThrowsAsync(new HttpRequestException());

            // Act
            var project = await _kepwareApiClient.Project.LoadProject(true);

            // Assert
            project.ShouldNotBeNull();
            project.Channels.ShouldBeNull();
        }
    }
}
