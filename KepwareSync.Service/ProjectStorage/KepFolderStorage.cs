using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using YamlDotNet.Serialization.NodeTypeResolvers;
using System.Security.AccessControl;
using Kepware.SyncService.Configuration;
using System.Text.Json;
using System.Reactive.Linq;
using Kepware.Api.Util;
using System.Collections.ObjectModel;

namespace Kepware.SyncService.ProjectStorage
{
    internal class KepFolderStorage : IProjectStorage
    {

        private readonly HashSet<string> m_ignoredFolder;
        private readonly YamlSerializer m_yamlSerializer;
        private readonly CsvTagSerializer m_csvTagSerializer;
        private readonly DataTypeEnumConverterProvider m_dataTypeEnumConverterProvider;
        private readonly ILogger<KepFolderStorage> m_logger;
        private readonly DirectoryInfo m_baseDirectory;
        private bool m_suppressEvents = false;

        public KepFolderStorage(ILogger<KepFolderStorage> logger, KepStorageOptions kepStorageOptions, YamlSerializer yamlSerializer, CsvTagSerializer csvTagSerializer)
        {
            m_logger = logger;
            m_yamlSerializer = yamlSerializer;
            m_csvTagSerializer = csvTagSerializer;
            m_dataTypeEnumConverterProvider = new DataTypeEnumConverterProvider();
            m_baseDirectory = new DirectoryInfo(kepStorageOptions.Directory ?? "ExportedYaml");
            if (!m_baseDirectory.Exists)
                m_baseDirectory.Create();
            m_ignoredFolder = new HashSet<string>([
                Path.Combine(m_baseDirectory.FullName, ".git"),
                ], StringComparer.InvariantCultureIgnoreCase);
        }

        public async Task<Project> LoadProject(bool blnLoadFullProject = true, CancellationToken cancellationToken = default)
        {
            var projectFile = Path.Combine(m_baseDirectory.FullName, "project.yaml");

            var project = await m_yamlSerializer.LoadFromYaml<Project>(projectFile, cancellationToken);
            if (project == null) return new Project();

            if (blnLoadFullProject)
            {
                project.Channels = await LoadChannels(blnLoadFullProject, cancellationToken);
            }

            return project;
        }

        public async Task<ChannelCollection> LoadChannels(bool blnLoadDeep = true, CancellationToken cancellationToken = default)
        {
            var channelDirs = m_baseDirectory.GetDirectories().Where(dir => !m_ignoredFolder.Contains(dir.FullName));
            var channels = new ChannelCollection();
            foreach (var channelDir in channelDirs)
            {
                channels.Add(await LoadChannel(channelDir.Name, blnLoadDeep, cancellationToken));
            }
            return channels;
        }

        public Task<Channel> LoadChannel(string channelName, bool blnLoadDeep = true, CancellationToken cancellationToken = default)
            => LoadChannel(new DirectoryInfo(Path.Combine(m_baseDirectory.FullName, channelName)), blnLoadDeep, cancellationToken);

        public async Task<Channel> LoadChannel(DirectoryInfo channelDir, bool blnLoadDeep = true, CancellationToken cancellationToken = default)
        {
            var channelFile = Path.Combine(channelDir.FullName, "channel.yaml");

            var channel = await m_yamlSerializer.LoadFromYaml<Channel>(channelFile, cancellationToken);
            if (channel == null) return new Channel();
            if (blnLoadDeep)
            {
                channel.Devices = await LoadDevices(channelDir, blnLoadDeep, cancellationToken);
            }
            return channel;
        }

        private async Task<DeviceCollection> LoadDevices(DirectoryInfo channelDir, bool blnLoadDeep = true, CancellationToken cancellationToken = default)
        {
            var devices = new DeviceCollection();
            var deviceDirs = channelDir.GetDirectories();
            foreach (var deviceDir in deviceDirs)
            {
                devices.Add(await LoadDevice(deviceDir, blnLoadDeep, cancellationToken));
            }
            return devices;
        }

        private async Task<Device> LoadDevice(DirectoryInfo deviceDir, bool blnLoadDeep = true, CancellationToken cancellationToken = default)
        {
            var deviceFile = Path.Combine(deviceDir.FullName, "device.yaml");
            var device = await m_yamlSerializer.LoadFromYaml<Device>(deviceFile, cancellationToken);
            if (device == null) return new Device();
            if (blnLoadDeep)
            {
                device.Tags = [.. await LoadTags(device, deviceDir, cancellationToken)];
                device.TagGroups = await LoadTagGroups(device, deviceDir, cancellationToken);
            }
            return device;
        }

        private async Task<DeviceTagGroupCollection?> LoadTagGroups(Device device, DirectoryInfo deviceDir, CancellationToken cancellationToken = default)
        {
            var tagGroups = new DeviceTagGroupCollection();
            var tagGroupDirs = deviceDir.GetDirectories();

            foreach (var grpDir in tagGroupDirs)
            {
                var tagGroupFile = Path.Combine(grpDir.FullName, "tagGroup.yaml");
                var tagGroup = await m_yamlSerializer.LoadFromYaml<DeviceTagGroup>(tagGroupFile, cancellationToken);
                tagGroup.Tags = [.. await LoadTags(device, grpDir, cancellationToken)];
                tagGroup.TagGroups = await LoadTagGroups(device, grpDir, cancellationToken);

                tagGroups.Add(tagGroup);
            }

            return tagGroups;
        }

        private Task<List<Tag>> LoadTags(Device device, DirectoryInfo tagDirectory, CancellationToken cancellationToken = default)
        {
            var tagFile = Path.Combine(tagDirectory.FullName, "tags.csv");
            if (!File.Exists(tagFile))
                return Task.FromResult(new List<Tag>());

            var dataTypeConverter = m_dataTypeEnumConverterProvider.GetDataTypeEnumConverter(device.GetDynamicProperty<string>(Properties.Channel.DeviceDriver));
            return m_csvTagSerializer.ImportTagsAsync(tagFile, dataTypeConverter, cancellationToken);
        }

        public async Task ExportProjecAsync(Project project, CancellationToken cancellationToken = default)
        {
            try
            {
                SuppressEvents();
                var projectFile = Path.Combine(m_baseDirectory.FullName, "project.yaml");
                await m_yamlSerializer.SaveAsYaml(projectFile, project, cancellationToken);
                await ExportChannelsAsync(project.Channels, cancellationToken);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error exporting project");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(1)); // Wait for file system to catch up
                ResumeEvents();
            }
        }
        protected async Task ExportChannelsAsync(ChannelCollection? channels, CancellationToken cancellationToken = default)
        {
            if (channels == null) return;

            try
            {
                var channelDirsToDelete = m_baseDirectory.GetDirectories()
                    .Where(dir => !m_ignoredFolder.Contains(dir.FullName))
                    .Select(dir => dir.Name)
                  .ToHashSet(StringComparer.OrdinalIgnoreCase);

                List<(string baseDir, IEnumerable<string> folderNames)> foldersToDelete = new();
                int exportedChannels = 0, exportedDevices = 0, exportedTags = 0;
                foreach (var channel in channels)
                {
                    var fileSaveName = channel.Name.EscapeDiskEntry();

                    var channelFolder = new DirectoryInfo(Path.Combine(m_baseDirectory.FullName, fileSaveName));
                    if (!channelFolder.Exists)
                        channelFolder.Create();
                    channelDirsToDelete.Remove(fileSaveName);
                    ++exportedChannels;
                    try
                    {
                        // Export Channel
                        var channelFile = Path.Combine(channelFolder.FullName, "channel.yaml");
                        await m_yamlSerializer.SaveAsYaml(channelFile, channel, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Error exporting channel '{ChannelName}' to YAML.", channel.Name);
                        continue;
                    }

                    if (channel.Devices != null)
                    {
                        var deviceDirsToDelete = new DirectoryInfo(channelFolder.FullName).GetDirectories().Select(dir => dir.Name)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var count = await ExportDevices(channel, channelFolder.FullName, deviceDirsToDelete, cancellationToken);

                        exportedDevices += count.exportedDevices;
                        exportedTags += count.exportedTags;

                        foldersToDelete.Add((channelFolder.FullName, deviceDirsToDelete));
                    }
                }

                try
                {
                    DeleteDirectories(m_baseDirectory.FullName, channelDirsToDelete);

                    foreach (var (baseDir, folderNames) in foldersToDelete)
                    {
                        DeleteDirectories(baseDir, folderNames);
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error deleting directories.");
                }

                // Log information: Exported channels to YAML successfully (including target directory, count of channels, devices, and tags).
                m_logger.LogInformation(
                    "Exported channels to YAML successfully. Target directory: {TargetDirectory}, Channels: {ChannelCount}, Devices: {DeviceCount}, Tags: {TagCount}",
                    m_baseDirectory.FullName,
                    exportedChannels,
                    exportedDevices,
                    exportedTags);

            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error exporting channels to YAML.");
            }
        }

        private void DeleteDirectories(string baseDir, IEnumerable<string> names)
        {
            // Delete all channel directories that are not in the current project   
            foreach (var dir in names)
            {
                var folderPath = Path.Combine(baseDir, dir);
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                    m_logger.LogInformation("Deleted folder: {FolderPath}", folderPath);
                }
            }
        }

        protected async Task ExportDevices(Channel channel, DeviceCollection? devices, CancellationToken cancellationToken = default)
        {
            string channelFolder = Path.Combine(m_baseDirectory.FullName, channel.Name.EscapeDiskEntry());

            var deviceDirsToDelete = new DirectoryInfo(channelFolder).GetDirectories().Select(dir => dir.Name)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
            await ExportDevices(channel, channelFolder, deviceDirsToDelete, cancellationToken).ConfigureAwait(false);
            DeleteDirectories(channelFolder, deviceDirsToDelete);
        }

        private async Task<(int exportedDevices, int exportedTags)> ExportDevices(Channel channel, string channelFolder, HashSet<string> deviceDirsToDelete, CancellationToken cancellationToken = default)
        {
            if (channel?.Devices == null)
                return (0, 0);

            int exportedDevices = 0, exportedTags = 0;

            foreach (var device in channel.Devices)
            {
                ++exportedDevices;

                string fileSaveName = device.Name.EscapeDiskEntry();

                var deviceFolder = Path.Combine(channelFolder, fileSaveName);
                if (!Directory.Exists(deviceFolder))
                    Directory.CreateDirectory(deviceFolder);

                var dataTypeConverter = m_dataTypeEnumConverterProvider.GetDataTypeEnumConverter(device.GetDynamicProperty<string>(Properties.Channel.DeviceDriver));
                deviceDirsToDelete.Remove(fileSaveName);

                try
                {
                    // Export Device
                    var deviceFile = Path.Combine(deviceFolder, "device.yaml");
                    await m_yamlSerializer.SaveAsYaml(deviceFile, device, cancellationToken).ConfigureAwait(false);

                    await m_csvTagSerializer.ExportTagsAsync(Path.Combine(deviceFolder, "tags.csv"), device.Tags, dataTypeConverter, cancellationToken);
                    exportedTags += device.Tags?.Count ?? 0;
                    exportedTags += await ExportTagGroups(deviceFolder, device.TagGroups, dataTypeConverter, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error exporting device '{DeviceName}' to YAML.", device.Name);
                }
            }
            return (exportedDevices, exportedTags);
        }

        private async Task<int> ExportTagGroups(string parentFolder, IEnumerable<DeviceTagGroup>? tagGroups, IDataTypeEnumConverter dataTypeConverter, CancellationToken cancellationToken = default)
        {
            var tagGrpDirsToDelete = new DirectoryInfo(parentFolder).GetDirectories().Select(dir => dir.Name)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int exportedTags = 0;
            if (tagGroups != null)
                foreach (var tagGroup in tagGroups)
                {
                    string fileSaveName = tagGroup.Name.EscapeDiskEntry();
                    var tagGroupFolder = Path.Combine(parentFolder, fileSaveName);
                    tagGrpDirsToDelete.Remove(fileSaveName);

                    // Export TagGroup
                    var tagGroupFile = Path.Combine(tagGroupFolder, "tagGroup.yaml");
                    await m_yamlSerializer.SaveAsYaml(tagGroupFile, tagGroup, cancellationToken).ConfigureAwait(false);

                    await m_csvTagSerializer.ExportTagsAsync(Path.Combine(tagGroupFolder, "tags.csv"), tagGroup.Tags, dataTypeConverter, cancellationToken).ConfigureAwait(false);
                    exportedTags += tagGroup.Tags?.Count ?? 0;

                    exportedTags += await ExportTagGroups(tagGroupFolder, tagGroup.TagGroups, dataTypeConverter, cancellationToken).ConfigureAwait(false);
                }
            DeleteDirectories(parentFolder, tagGrpDirsToDelete);
            return exportedTags;
        }

        protected void SuppressEvents()
        {
            m_suppressEvents = true;
        }

        protected void ResumeEvents()
        {
            m_suppressEvents = false;
        }
        private bool SupressEvent(FileSystemEventArgs e)
        {
            if (m_suppressEvents)
            {
                return true;
            }
            else
            {
                return m_ignoredFolder.Any(dir => e.FullPath.StartsWith(dir, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public IObservable<StorageChangeEvent> ObserveChanges()
        {
            return Observable.Create<StorageChangeEvent>(observer =>
                {
                    var watcher = new FileSystemWatcher(m_baseDirectory.FullName)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true,
                    };

                    // Event handlers
                    FileSystemEventHandler onChanged = (s, e) =>
                    {
                        if (!SupressEvent(e))
                        {
                            observer.OnNext(new StorageChangeEvent(StorageChangeEvent.ChangeType.changed));
                        }
                    };

                    FileSystemEventHandler onCreated = (s, e) =>
                    {
                        if (!SupressEvent(e))
                        {
                            observer.OnNext(new StorageChangeEvent(StorageChangeEvent.ChangeType.added));
                        }
                    };

                    FileSystemEventHandler onDeleted = (s, e) =>
                    {
                        if (!SupressEvent(e))
                        {
                            observer.OnNext(new StorageChangeEvent(StorageChangeEvent.ChangeType.removed));
                        }
                    };

                    RenamedEventHandler onRenamed = (s, e) =>
                    {
                        if (!SupressEvent(e))
                        {
                            observer.OnNext(new StorageChangeEvent(StorageChangeEvent.ChangeType.changed));
                        }
                    };

                    // Subscribe to events
                    watcher.Changed += onChanged;
                    watcher.Created += onCreated;
                    watcher.Deleted += onDeleted;
                    watcher.Renamed += onRenamed;

                    // Cleanup
                    return () =>
                    {
                        watcher.Changed -= onChanged;
                        watcher.Created -= onCreated;
                        watcher.Deleted -= onDeleted;
                        watcher.Renamed -= onRenamed;

                        watcher.EnableRaisingEvents = false;
                        watcher.Dispose();
                    };
                }
            );
        }
    }
}
