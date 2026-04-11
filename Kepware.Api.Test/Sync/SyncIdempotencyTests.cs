#if NET10_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Test.ApiClient;
using Kepware.Api.Util;
using Kepware.SyncService;
using Kepware.SyncService.Configuration;
using Kepware.SyncService.ProjectStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Contrib.HttpClient;
using Shouldly;

namespace Kepware.Api.Test.Sync
{
    public class SyncIdempotencyTests : TestApiClientBase
    {
        private readonly YamlSerializer _yamlSerializer = new(Mock.Of<ILogger<YamlSerializer>>());
        private readonly CsvTagSerializer _csvTagSerializer = new(Mock.Of<ILogger<CsvTagSerializer>>());

        [Fact]
        public async Task KepFolderStorage_ProjectRoundtrip_ShouldBeIdempotent()
        {
            var project = CreateStorageProjectGraph();
            var tempRoot = CreateTempDirectory(nameof(KepFolderStorage_ProjectRoundtrip_ShouldBeIdempotent));

            try
            {
                var storage = CreateStorage(tempRoot);

                await storage.ExportProjecAsync(project);
                var firstLoad = await storage.LoadProject(true);
                AssignOwners(firstLoad);

                var firstProjectYaml = await File.ReadAllTextAsync(Path.Combine(tempRoot, "project.yaml"));
                var firstDeviceTagsCsv = await File.ReadAllTextAsync(Path.Combine(tempRoot, "Channel_Main", "Device_Main", "tags.csv"));
                AssertProjectsEquivalent(project, firstLoad);

                await storage.ExportProjecAsync(firstLoad);
                var secondLoad = await storage.LoadProject(true);
                AssignOwners(secondLoad);

                AssertProjectsEquivalent(firstLoad, secondLoad);
                AssertProjectsEquivalent(project, secondLoad);

                (await File.ReadAllTextAsync(Path.Combine(tempRoot, "project.yaml"))).ShouldBe(firstProjectYaml);
                (await File.ReadAllTextAsync(Path.Combine(tempRoot, "Channel_Main", "Device_Main", "tags.csv"))).ShouldBe(firstDeviceTagsCsv);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        [Fact]
        public async Task CompareAndApply_RoundtripReloadedProject_SecondRun_ShouldReportNoChanges()
        {
            var tempRoot = CreateTempDirectory(nameof(CompareAndApply_RoundtripReloadedProject_SecondRun_ShouldReportNoChanges));

            try
            {
                var storage = CreateStorage(tempRoot);
                var sourceProject = CreateProjectGraph();
                await storage.ExportProjecAsync(sourceProject);

                var roundtrippedProject = await storage.LoadProject(true);
                AssignOwners(roundtrippedProject);

                var targetProject = await roundtrippedProject.CloneAsync();
                AssignOwners(targetProject);

                var sourceTag = roundtrippedProject.Channels![0].Devices![0].Tags![0];
                var targetTag = targetProject.Channels![0].Devices![0].Tags![0];
                targetTag.Description = "Outdated description";

                var tagEndpoint = TEST_ENDPOINT + "/config/v1/project/channels/Channel_Main/devices/Device_Main/tags/Tag_Main";
                _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, tagEndpoint)
                    .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(targetTag, KepJsonContext.Default.Tag), "application/json");
                _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, tagEndpoint)
                    .ReturnsResponse(HttpStatusCode.OK);

                var firstResult = await _kepwareApiClient.Project.CompareAndApplyDetailedAsync(roundtrippedProject, targetProject);
                firstResult.Updates.ShouldBe(1);
                firstResult.Inserts.ShouldBe(0);
                firstResult.Deletes.ShouldBe(0);
                firstResult.Failures.ShouldBe(0);

                var appliedProject = await roundtrippedProject.CloneAsync();
                AssignOwners(appliedProject);

                var secondResult = await _kepwareApiClient.Project.CompareAndApplyDetailedAsync(roundtrippedProject, appliedProject);
                secondResult.Updates.ShouldBe(0);
                secondResult.Inserts.ShouldBe(0);
                secondResult.Deletes.ShouldBe(0);
                secondResult.Failures.ShouldBe(0);
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        [Fact]
        public async Task CompareAndApply_ProjectHashMismatchWithoutProjectPropertyDiff_ShouldReportNoChanges()
        {
            ConfigureConnectedClient();

            var sourceProject = CreateProjectPropertiesOnly("Desired description");
            _ = sourceProject.Hash;
            sourceProject.Channels = [CreateTestChannel("Channel_Main", "Simulator")];

            var targetProject = await sourceProject.CloneAsync();
            AssignOwners(targetProject);

            var targetProjectJson = JsonSerializer.Serialize(targetProject, KepJsonContext.Default.Project);
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project")
                .ReturnsResponse(HttpStatusCode.OK, targetProjectJson, "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, TEST_ENDPOINT + "/config/v1/project")
                .ReturnsResponse(HttpStatusCode.OK);

            var result = await _kepwareApiClient.Project.CompareAndApplyDetailedAsync(sourceProject, targetProject);

            result.Updates.ShouldBe(0);
            result.Inserts.ShouldBe(0);
            result.Deletes.ShouldBe(0);
            result.Failures.ShouldBe(0);
        }

        [Fact]
        public async Task SyncService_DiskToPrimaryFollowedByPrimarySync_ShouldNotLoopWhenNoEffectiveChangesRemain()
        {
            ConfigureConnectedClient();

            var diskProject = CreateProjectPropertiesOnly("Desired description");
            var staleProject = CreateProjectPropertiesOnly("Previous description");
            var updatedProject = CreateProjectPropertiesOnly("Desired description");

            var staleProjectJson = JsonSerializer.Serialize(staleProject, KepJsonContext.Default.Project);
            var staleFullProjectJson = JsonSerializer.Serialize(new JsonProjectRoot { Project = staleProject }, KepJsonContext.Default.JsonProjectRoot);
            var updatedProjectJson = JsonSerializer.Serialize(updatedProject, KepJsonContext.Default.Project);
            var updatedFullProjectJson = JsonSerializer.Serialize(new JsonProjectRoot { Project = updatedProject }, KepJsonContext.Default.JsonProjectRoot);

            var firstStorage = new FakeProjectStorage(diskProject);
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project")
                .ReturnsResponse(HttpStatusCode.OK, staleProjectJson, "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project?content=serialize")
                .ReturnsResponse(HttpStatusCode.OK, staleFullProjectJson, "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Put, TEST_ENDPOINT + "/config/v1/project")
                .ReturnsResponse(HttpStatusCode.OK);

            using var firstService = CreateSyncService(firstStorage);
            await firstService.SyncFromLocalFileAsync();

            GetQueuedEvents(firstService).Select(change => change.Source).ShouldBe([ChangeSource.PrimaryKepServer]);

            var followUpStorage = new FakeProjectStorage(diskProject);
            _httpMessageHandlerMock.Reset();
            ConfigureConnectedClient();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project")
                .ReturnsResponse(HttpStatusCode.OK, updatedProjectJson, "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project?content=serialize")
                .ReturnsResponse(HttpStatusCode.OK, updatedFullProjectJson, "application/json");

            using var followUpService = CreateSyncService(followUpStorage);
            await followUpService.SyncFromPrimaryKepServerAsync();

            followUpStorage.ExportCount.ShouldBe(1);
            followUpStorage.LastExportedProject.ShouldNotBeNull();
            GetQueuedEvents(followUpService).ShouldBeEmpty();

            var stableStorage = new FakeProjectStorage(diskProject);
            _httpMessageHandlerMock.Reset();
            ConfigureConnectedClient();
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project")
                .ReturnsResponse(HttpStatusCode.OK, updatedProjectJson, "application/json");
            _httpMessageHandlerMock.SetupRequest(HttpMethod.Get, TEST_ENDPOINT + "/config/v1/project?content=serialize")
                .ReturnsResponse(HttpStatusCode.OK, updatedFullProjectJson, "application/json");

            using var stableService = CreateSyncService(stableStorage);
            await stableService.SyncFromLocalFileAsync();

            GetQueuedEvents(stableService).ShouldBeEmpty();
        }

        private KepFolderStorage CreateStorage(string directory)
        {
            return new KepFolderStorage(
                NullLogger<KepFolderStorage>.Instance,
                new KepStorageOptions { Directory = directory },
                _yamlSerializer,
                _csvTagSerializer);
        }

        private Kepware.SyncService.SyncService CreateSyncService(IProjectStorage storage)
        {
            return new Kepware.SyncService.SyncService(
                _kepwareApiClient,
                [],
                storage,
                new KepSyncOptions
                {
                    SyncDirection = SyncDirection.KepwareToDiskAndSecondary,
                    SyncMode = SyncMode.TwoWay,
                    SyncThrottlingMs = 0
                },
                Mock.Of<ILogger<Kepware.SyncService.SyncService>>());
        }

        private static string CreateTempDirectory(string testName)
        {
            var directory = Path.Combine(Path.GetTempPath(), testName, Path.GetRandomFileName());
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static Project CreateProjectPropertiesOnly(string description)
        {
            var project = new Project
            {
                Description = description
            };
            project.ProjectProperties.Title = "Sync project";
            return project;
        }

        private static Project CreateProjectGraph()
        {
            var project = new Project();

            var channel = new Channel
            {
                Name = "Channel_Main",
                Description = "Main channel",
                DeviceDriver = "Simulator"
            };
            channel.SetDynamicProperty(Properties.NonUpdatable.ChannelUniqueId, 1001L);

            var device = new Device
            {
                Name = "Device_Main",
                Description = "Main device",
                Channel = channel,
                Tags =
                [
                    CreateTag("Tag_Main", "RAMP", "Primary tag", scalingEnabled: true)
                ]
            };
            device.SetDynamicProperty(Properties.NonUpdatable.DeviceUniqueId, 2001L);
            device.SetDynamicProperty(Properties.Channel.DeviceDriver, "Simulator");
            device.SetDynamicProperty(Properties.Device.DeviceDriver, "Simulator");
            channel.Devices = [device];
            project.Channels = [channel];

            AssignOwners(project);
            return project;
        }

        private static Project CreateStorageProjectGraph()
        {
            var project = CreateProjectGraph();
            project.Description = "Roundtrip project";
            project.ProjectProperties.Title = "Roundtrip title";
            return project;
        }

        private static Tag CreateTag(string name, string address, string description, bool scalingEnabled = false)
        {
            var tag = new Tag
            {
                Name = name,
                Description = description,
                TagAddress = address,
                DataType = scalingEnabled ? 8 : 1,
                ReadWriteAccess = scalingEnabled ? 1 : 0,
                ScanRateMilliseconds = scalingEnabled ? 250 : 100,
                ScalingType = scalingEnabled ? 1 : 0
            };

            if (scalingEnabled)
            {
                tag.ScalingRawLow = 0;
                tag.ScalingRawHigh = 100;
                tag.ScalingScaledLow = 0;
                tag.ScalingScaledHigh = 1000;
                tag.ScalingScaledDataType = 8;
                tag.ScalingClampLow = true;
                tag.ScalingClampHigh = false;
                tag.ScalingUnits = "psi";
                tag.ScalingNegateValue = true;
            }

            return tag;
        }

        private static void AssignOwners(Project project)
        {
            foreach (var channel in project.Channels ?? [])
            {
                foreach (var device in channel.Devices ?? [])
                {
                    device.Channel = channel;

                    foreach (var tag in device.Tags ?? [])
                    {
                        tag.Owner = device;
                    }

                }
            }
        }

        private static void AssertProjectsEquivalent(Project expected, Project actual)
        {
            actual.Description.ShouldBe(expected.Description);
            actual.ProjectProperties.Title.ShouldBe(expected.ProjectProperties.Title);

            var channelCompare = EntityCompare.Compare<ChannelCollection, Channel>(expected.Channels, actual.Channels);
            channelCompare.ChangedItems.ShouldBeEmpty();
            channelCompare.ItemsOnlyInLeft.ShouldBeEmpty();
            channelCompare.ItemsOnlyInRight.ShouldBeEmpty();
            channelCompare.UnchangedItems.Count.ShouldBe(expected.Channels?.Count ?? 0);

            foreach (var expectedChannel in expected.Channels ?? [])
            {
                var actualChannel = actual.Channels!.Single(channel => channel.Name == expectedChannel.Name);
                actualChannel.Hash.ShouldBe(expectedChannel.Hash);

                var deviceCompare = EntityCompare.Compare<DeviceCollection, Device>(expectedChannel.Devices, actualChannel.Devices);
                deviceCompare.ChangedItems.ShouldBeEmpty();
                deviceCompare.ItemsOnlyInLeft.ShouldBeEmpty();
                deviceCompare.ItemsOnlyInRight.ShouldBeEmpty();
                deviceCompare.UnchangedItems.Count.ShouldBe(expectedChannel.Devices?.Count ?? 0);

                foreach (var expectedDevice in expectedChannel.Devices ?? [])
                {
                    var actualDevice = actualChannel.Devices!.Single(device => device.Name == expectedDevice.Name);
                    actualDevice.Hash.ShouldBe(expectedDevice.Hash);

                    var tagCompare = EntityCompare.Compare<DeviceTagCollection, Tag>(expectedDevice.Tags, actualDevice.Tags);
                    tagCompare.ChangedItems.ShouldBeEmpty();
                    tagCompare.ItemsOnlyInLeft.ShouldBeEmpty();
                    tagCompare.ItemsOnlyInRight.ShouldBeEmpty();
                    tagCompare.UnchangedItems.Count.ShouldBe(expectedDevice.Tags?.Count ?? 0);

                    (actualDevice.TagGroups?.Count ?? 0).ShouldBe(expectedDevice.TagGroups?.Count ?? 0);
                }
            }
        }

        private static ChangeEvent[] GetQueuedEvents(Kepware.SyncService.SyncService service)
        {
            var queueField = typeof(Kepware.SyncService.SyncService).GetField("m_changeQueue", BindingFlags.Instance | BindingFlags.NonPublic);
            queueField.ShouldNotBeNull();

            var queue = queueField!.GetValue(service) as ConcurrentQueue<ChangeEvent>;
            queue.ShouldNotBeNull();

            return queue!.ToArray();
        }

        private sealed class FakeProjectStorage : IProjectStorage
        {
            private readonly Project _projectToLoad;

            public FakeProjectStorage(Project projectToLoad)
            {
                _projectToLoad = projectToLoad;
            }

            public int ExportCount { get; private set; }

            public Project? LastExportedProject { get; private set; }

            public Task<Project> LoadProject(bool blnLoadFullProject = true, CancellationToken cancellationToken = default)
                => Task.FromResult(_projectToLoad);

            public async Task ExportProjecAsync(Project project, CancellationToken cancellationToken = default)
            {
                ExportCount++;
                LastExportedProject = await project.CloneAsync(cancellationToken);
            }

            public IObservable<StorageChangeEvent> ObserveChanges() => new EmptyObservable<StorageChangeEvent>();
        }

        private sealed class EmptyObservable<T> : IObservable<T>
        {
            public IDisposable Subscribe(IObserver<T> observer) => new EmptyDisposable();
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
#endif
