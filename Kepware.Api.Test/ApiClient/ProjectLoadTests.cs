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
using Xunit.Sdk;

namespace Kepware.Api.Test.ApiClient
{
    public class ProjectLoadTests : TestApiClientBase
    {

        [Theory]
        [InlineData("KEPServerEX", "12", 6, 17, true)]
        [InlineData("KEPServerEX", "12", 6, 16, false)]
        [InlineData("ThingWorxKepwareEdge", "13", 1, 10, true)]
        [InlineData("ThingWorxKepwareEdge", "13", 1, 9, false)]
        [InlineData("UnknownProduct", "99", 10, 0, false)]
        public async Task LoadProject_ShouldLoadCorrectly_BasedOnProductSupport(
            string productName, string productId, int majorVersion, int minorVersion, bool supportsJsonLoad)
        {
            // This test will validate that the LoadProjectAsync method correctly loads the project structure and
            // content based on whether the connected server version supports JsonProjectLoad. It will compare the loaded project against expected test data to ensure accuracy.
            // For servers that support JsonProjectLoad, the test will configure the mock server to serve a full JSON project
            // and validate that the loaded project matches the test data exactly.

            // Arrange
            ConfigureConnectedClient(productName, productId, majorVersion, minorVersion);

            if (supportsJsonLoad)
            {
                await ConfigureToServeFullProject();
            }
            else
            {
                await ConfigureToServeEndpoints();
            }

            // Act
            var project = await _kepwareApiClient.Project.LoadProjectAsync(blnLoadFullProject: true);

            // Assert
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

        [Theory]
        [InlineData("KEPServerEX", "12", 6, 17, true)]
        [InlineData("ThingWorxKepwareServer", "12", 6, 17, true)]
        [InlineData("ThingWorxKepwareEdge", "13", 1, 10, true)]
        [InlineData("Kepware Edge", "13", 1, 0, true)]
        public async Task LoadProject_ShouldLoadCorrectly_Serialize_BasedOnProductSupport(
            string productName, string productId, int majorVersion, int minorVersion, bool supportsJsonLoad)
        {
            // This test will validate that the LoadProjectAsync method correctly loads the project structure using the optimized recursion method.
            // It will compare the loaded project against expected test data to ensure accuracy. The test will configure the mock server to serve
            // endpoints to support an optimized recursion load and validate that the loaded project matches the test data exactly.

            // Arrange
            ConfigureConnectedClient(productName, productId, majorVersion, minorVersion);

            if (supportsJsonLoad)
            {
                await ConfigureToServeEndpoints();
            }
            else
            {
                // Skip this test case at runtime because it expects the server to serve a full JSON project.
                throw SkipException.ForSkip($"Product {productName} v{majorVersion}.{minorVersion} (id={productId}) does not support JSON project load. Skipping full-project test case.");
            }

            // Override the tag limit to ensure that we are testing the optimized recursion and selectively load objects based on the tag limit.
            // See _data/simdemo_en.json and json chunks in _data/projectLoadSerializeData for data that is served by the mock server for this test.
            var tagLimitOverride = 100;


            // Act
            var project = await _kepwareApiClient.Project.LoadProjectAsync(blnLoadFullProject: true, projectLoadTagLimit: tagLimitOverride);


            // Assert
            // Optimized recursion is done for this test, which will result in false.
            project.IsLoadedByProjectLoadService.ShouldBeFalse();

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

            // Verify expected number of calls to the project load endpoints to ensure that the optimized recursion is selectively loading objects based on the tag limit.
            foreach (var uri in _optimizedRecursionUris)
            {
                _httpMessageHandlerMock.VerifyRequest(HttpMethod.Get, uri);
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

            var project = await _kepwareApiClient.Project.LoadProjectAsync(blnLoadFullProject: false);

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
            var project = await _kepwareApiClient.Project.LoadProjectAsync(true);

            // Assert
            project.ShouldNotBeNull();
            project.Channels.ShouldBeNull();
        }
    }
}
