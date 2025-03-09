using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kepware.Api.ClientHandler
{
    /// <summary>
    /// Handles operations related to projects in the Kepware server.
    /// </summary>
    public class ProjectApiHandler
    {
        private const string ENDPONT_FULL_PROJECT = "/config/v1/project?content=serialize";

        private readonly KepwareApiClient m_kepwareApiClient;
        private readonly ILogger<ProjectApiHandler> m_logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectApiHandler"/> class.
        /// </summary>
        /// <param name="kepwareApiClient">The Kepware API client.</param>
        /// <param name="logger">The logger instance.</param>
        public ProjectApiHandler(KepwareApiClient kepwareApiClient, ILogger<ProjectApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_logger = logger;
        }

        #region CompareAndApply
        /// <summary>
        /// Compares the source project with the project from the API and applies the changes to the API.
        /// </summary>
        /// <param name="sourceProject">The source project to compare.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the counts of inserts, updates, and deletes.</returns>
        public async Task<(int inserts, int updates, int deletes)> CompareAndApply(Project sourceProject, CancellationToken cancellationToken = default)
        {
            var projectFromApi = await LoadProject(blnLoadFullProject: true, cancellationToken: cancellationToken);
            await projectFromApi.Cleanup(m_kepwareApiClient, true, cancellationToken).ConfigureAwait(false);
            return await CompareAndApply(sourceProject, projectFromApi, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Compares the source project with the project from the API and applies the changes to the API.
        /// </summary>
        /// <param name="sourceProject">The source project to compare.</param>
        /// <param name="projectFromApi">The project loaded from the API.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the counts of inserts, updates, and deletes.</returns>
        public async Task<(int inserts, int updates, int deletes)> CompareAndApply(Project sourceProject, Project projectFromApi, CancellationToken cancellationToken = default)
        {
            if (sourceProject.Hash != projectFromApi.Hash)
            {
                //TODO update project
                m_logger.LogInformation("[not implemented] Project has changed. Updating project...");
            }
            int inserts = 0, updates = 0, deletes = 0;

            var channelCompare = await m_kepwareApiClient.GenericConfig.CompareAndApply<ChannelCollection, Channel>(sourceProject.Channels, projectFromApi.Channels,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            updates += channelCompare.ChangedItems.Count;
            inserts += channelCompare.ItemsOnlyInLeft.Count;
            deletes += channelCompare.ItemsOnlyInRight.Count;

            foreach (var channel in channelCompare.UnchangedItems.Concat(channelCompare.ChangedItems))
            {
                var deviceCompare = await m_kepwareApiClient.GenericConfig.CompareAndApply<DeviceCollection, Device>(channel.Left!.Devices, channel.Right!.Devices, channel.Right,
                cancellationToken: cancellationToken).ConfigureAwait(false);

                updates += deviceCompare.ChangedItems.Count;
                inserts += deviceCompare.ItemsOnlyInLeft.Count;
                deletes += deviceCompare.ItemsOnlyInRight.Count;

                foreach (var device in deviceCompare.UnchangedItems.Concat(deviceCompare.ChangedItems))
                {
                    var tagCompare = await m_kepwareApiClient.GenericConfig.CompareAndApply<DeviceTagCollection, Tag>(device.Left!.Tags, device.Right!.Tags, device.Right, cancellationToken).ConfigureAwait(false);

                    updates += tagCompare.ChangedItems.Count;
                    inserts += tagCompare.ItemsOnlyInLeft.Count;
                    deletes += tagCompare.ItemsOnlyInRight.Count;

                    var tagGroupCompare = await m_kepwareApiClient.GenericConfig.CompareAndApply<DeviceTagGroupCollection, DeviceTagGroup>(device.Left!.TagGroups, device.Right!.TagGroups, device.Right, cancellationToken).ConfigureAwait(false);

                    updates += tagGroupCompare.ChangedItems.Count;
                    inserts += tagGroupCompare.ItemsOnlyInLeft.Count;
                    deletes += tagGroupCompare.ItemsOnlyInRight.Count;


                    foreach (var tagGroup in tagGroupCompare.UnchangedItems.Concat(tagGroupCompare.ChangedItems))
                    {
                        var tagGroupTagCompare = await m_kepwareApiClient.GenericConfig.CompareAndApply<DeviceTagGroupTagCollection, Tag>(tagGroup.Left!.Tags, tagGroup.Right!.Tags, tagGroup.Right, cancellationToken).ConfigureAwait(false);

                        updates += tagGroupTagCompare.ChangedItems.Count;
                        inserts += tagGroupTagCompare.ItemsOnlyInLeft.Count;
                        deletes += tagGroupTagCompare.ItemsOnlyInRight.Count;

                        if (tagGroup.Left?.TagGroups != null)
                        {
                            var result = await RecusivlyCompareTagGroup(tagGroup.Left!.TagGroups, tagGroup.Right!.TagGroups, tagGroup.Right, cancellationToken).ConfigureAwait(false);
                            updates += result.updates;
                            inserts += result.inserts;
                            deletes += result.deletes;
                        }
                    }
                }
            }

            return (inserts, updates, deletes);
        }
        #endregion

        #region LoadProject
        /// <summary>
        /// Loads the project from the Kepware server.
        /// </summary>
        /// <param name="blnLoadFullProject">Indicates whether to load the full project.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded project.</returns>
        public async Task<Project> LoadProject(bool blnLoadFullProject = false, CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var productInfo = await m_kepwareApiClient.GetProductInfoAsync(cancellationToken).ConfigureAwait(false);

            if (blnLoadFullProject && productInfo?.SupportsJsonProjectLoadService == true)
            {
                try
                {
                    var response = await m_kepwareApiClient.HttpClient.GetAsync(ENDPONT_FULL_PROJECT, cancellationToken).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var prjRoot = await JsonSerializer.DeserializeAsync(
                            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                            KepJsonContext.Default.JsonProjectRoot, cancellationToken).ConfigureAwait(false);

                        if (prjRoot?.Project != null)
                        {
                            prjRoot.Project.IsLoadedByProjectLoadService = true;

                            if (prjRoot.Project.Channels != null)
                                foreach (var channel in prjRoot.Project.Channels)
                                {
                                    if (channel.Devices != null)
                                        foreach (var device in channel.Devices)
                                        {
                                            device.Owner = channel;

                                            if (device.Tags != null)
                                                foreach (var tag in device.Tags)
                                                    tag.Owner = device;

                                            if (device.TagGroups != null)
                                                SetOwnerRecursive(device.TagGroups, device);
                                        }
                                }

                            m_logger.LogInformation("Loaded project via JsonProjectLoad Service in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                            return prjRoot.Project;
                        }
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_kepwareApiClient.HttpClient.BaseAddress);
                    m_kepwareApiClient.OnHttpRequestException(httpEx);
                }

                m_logger.LogWarning("Failed to load project");
                return new Project();
            }
            else
            {
                var project = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Project>(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (project == null)
                {
                    m_logger.LogWarning("Failed to load project");
                    project = new Project();
                }
                else if (blnLoadFullProject)
                {
                    project.Channels = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<ChannelCollection, Channel>(cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (project.Channels != null)
                    {
                        int totalChannelCount = project.Channels.Count;
                        int loadedChannelCount = 0;
                        await Task.WhenAll(project.Channels.Select(async channel =>
                        {
                            channel.Devices = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceCollection, Device>(channel, cancellationToken).ConfigureAwait(false);

                            if (channel.Devices != null)
                            {
                                await Task.WhenAll(channel.Devices.Select(async device =>
                                {
                                    device.Tags = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(device, cancellationToken: cancellationToken).ConfigureAwait(false);
                                    device.TagGroups = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device, cancellationToken: cancellationToken).ConfigureAwait(false);

                                    if (device.TagGroups != null)
                                    {
                                        await LoadTagGroupsRecursiveAsync(device.TagGroups, cancellationToken: cancellationToken).ConfigureAwait(false);
                                    }
                                }));
                            }
                            // Log information, loaded channel <Name> x of y
                            loadedChannelCount++;
                            if (totalChannelCount == 1)
                            {
                                m_logger.LogInformation("Loaded channel {ChannelName}", channel.Name);
                            }
                            else
                            {
                                m_logger.LogInformation("Loaded channel {ChannelName} {LoadedChannelCount} of {TotalChannelCount}", channel.Name, loadedChannelCount, totalChannelCount);
                            }

                        }));
                    }

                    m_logger.LogInformation("Loaded project in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                }

                return project;
            }
        }
        #endregion

        #region Channels
        #region GetOrCreateChannelAsync
        /// <summary>
        /// Gets or creates a channel with the specified name and driver.
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        /// <param name="driverName">The name of the driver.</param>
        /// <param name="properties">The properties to set on the channel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created or loaded channel.</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name or driver name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the channel cannot be created or loaded.</exception>
        public async Task<Channel> GetOrCreateChannelAsync(string name, string driverName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(driverName))
                throw new ArgumentException("Driver name cannot be null or empty", nameof(driverName));

            var channel = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>(name, cancellationToken: cancellationToken);

            if (channel == null)
            {
                channel = await CreateChannelAsync(name, driverName, properties, cancellationToken);
                if (channel != null)
                {
                    if (properties != null)
                    {
                        var currentHash = channel.Hash;
                        foreach (var property in properties)
                        {
                            channel.SetDynamicProperty(property.Key, property.Value);
                        }

                        if (currentHash != channel.Hash)
                        {
                            await m_kepwareApiClient.GenericConfig.UpdateItemAsync(channel, cancellationToken: cancellationToken);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Failed to create or load channel '{name}'");
                }
            }

            return channel;
        }
        #endregion

        #region CreateChannelAsync

        /// <summary>
        /// Creates a new channel with the specified name and driver.
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        /// <param name="driverName">The name of the driver.</param>
        /// <param name="properties">The properties to set on the channel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created channel, or null if creation failed.</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name or driver name is null or empty.</exception>
        public async Task<Channel?> CreateChannelAsync(string name, string driverName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(driverName))
                throw new ArgumentException("Driver name cannot be null or empty", nameof(driverName));

            var channel = new Channel { Name = name };
            channel.SetDynamicProperty(Properties.Channel.DeviceDriver, driverName);
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    channel.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<ChannelCollection, Channel>(channel, cancellationToken: cancellationToken))
            {
                return channel;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region UpdateChannelAsync
        /// <summary>
        /// Updates the specified channel.
        /// </summary>
        /// <param name="channel">The channel to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the update was successful.</returns>
        public Task<bool> UpdateChannelAsync(Channel channel, CancellationToken cancellationToken = default)
            => m_kepwareApiClient.GenericConfig.UpdateItemAsync(channel, oldItem: null, cancellationToken);
        #endregion

        #region DeleteChannelAsync
        /// <summary>
        /// Deletes the specified channel.
        /// </summary>
        /// <param name="channel">The channel to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the channel is null.</exception>
        public Task<bool> DeleteChannelAsync(Channel channel, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync(channel, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes the channel with the specified name.
        /// </summary>
        /// <param name="channelName">The name of the channel to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name is null or empty.</exception>
        public Task<bool> DeleteChannelAsync(string channelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(channelName))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(channelName));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync<Channel>(channelName, cancellationToken: cancellationToken);
        }
        #endregion
        #endregion

        #region Devices
        #region GetOrCreateDeviceAsync
        /// <summary>
        /// Gets or creates a device with the specified name and driver in the specified channel.
        /// </summary>
        /// <param name="channel">The channel to which the device belongs.</param>
        /// <param name="name">The name of the device.</param>
        /// <param name="driverName">The name of the driver.</param>
        /// <param name="properties">The properties to set on the device.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created or loaded device.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the channel is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the device name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the device cannot be created or loaded.</exception>
        public async Task<Device> GetOrCreateDeviceAsync(Channel channel, string name, string? driverName = default, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Device name cannot be null or empty", nameof(name));

            var device = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Device>(name, channel, cancellationToken: cancellationToken);

            if (device == null)
            {
                device = await CreateDeviceAsync(channel, name, driverName, properties, cancellationToken);
                if (device != null)
                {
                    if (properties != null)
                    {
                        var currentHash = device.Hash;
                        foreach (var property in properties)
                        {
                            device.SetDynamicProperty(property.Key, property.Value);
                        }

                        if (currentHash != device.Hash)
                        {
                            await m_kepwareApiClient.GenericConfig.UpdateItemAsync(device, cancellationToken: cancellationToken);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Failed to create or load device '{name}'");
                }
            }
            else
            {
                device.Tags = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(device, cancellationToken: cancellationToken).ConfigureAwait(false);
                device.TagGroups = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (device.TagGroups != null)
                {
                    await LoadTagGroupsRecursiveAsync(device.TagGroups, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            return device;
        }
        #endregion

        #region CreateDeviceAsync
        /// <summary>
        /// Creates a new device with the specified name and driver in the specified channel.
        /// </summary>
        /// <param name="channel">The channel to which the device belongs.</param>
        /// <param name="name">The name of the device.</param>
        /// <param name="driverName">The name of the driver.</param>
        /// <param name="properties">The properties to set on the device.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created device, or null if creation failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the channel is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the device name is null or empty.</exception>
        public async Task<Device?> CreateDeviceAsync(Channel channel, string name, string? driverName = default, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Device name cannot be null or empty", nameof(name));

            var device = new Device { Name = name, Owner = channel };

            driverName = driverName ?? channel.GetDynamicProperty<string>(Properties.Channel.DeviceDriver);

            if (string.IsNullOrEmpty(driverName))
                throw new ArgumentException("Driver name cannot be null or empty", nameof(driverName));


            device.SetDynamicProperty(Properties.Channel.DeviceDriver, driverName);

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    device.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<DeviceCollection, Device>(device, channel, cancellationToken: cancellationToken))
            {
                return device;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region UpdateDeviceAsync
        /// <summary>
        /// Updates the specified device and optionally its tags and tag groups.
        /// </summary>
        /// <param name="device">The device to update.</param>
        /// <param name="updateTagsAndTagGroups">Indicates whether to update the tags and tag groups of the device.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the update was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the device or its channel is null.</exception>
        public async Task<bool> UpdateDeviceAsync(Device device, bool updateTagsAndTagGroups = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(device.Channel);

            var endpoint = EndpointResolver.ResolveEndpoint<Device>(device);
            var currentDevice = await m_kepwareApiClient.GenericConfig.LoadEntityByEndpointAsync<Device>(endpoint, cancellationToken: cancellationToken).ConfigureAwait(false);

            bool blnRet = true;

            if (currentDevice != null)
            {
                if (currentDevice.Hash != device.Hash)
                {
                    blnRet = await m_kepwareApiClient.GenericConfig.UpdateItemAsync(endpoint, device, currentDevice, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    //properties are equal, check tags and tag groups
                }

                if (updateTagsAndTagGroups)
                {
                    await m_kepwareApiClient.GenericConfig.CompareAndApply<DeviceTagCollection, Tag>(device.Tags, currentDevice.Tags, device, cancellationToken: cancellationToken);

                    if (device.TagGroups != null)
                        await RecusivlyCompareTagGroup(device.TagGroups, currentDevice.TagGroups, device, cancellationToken);
                }
            }
            else
            {
                m_logger.LogWarning("Could not update device, unable to load device {DeviceName} in channel {ChannelName}", device.Name, device.Channel.Name);
                blnRet = false;
            }
            return blnRet;
        }
        #endregion

        #region DeleteDeviceAsync
        /// <summary>
        /// Deletes the specified device.
        /// </summary>
        /// <param name="device">The device to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the device is null.</exception>
        public Task<bool> DeleteDeviceAsync(Device device, CancellationToken cancellationToken = default)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync(device, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes the device with the specified name in the specified channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to which the device belongs.</param>
        /// <param name="deviceName">The name of the device to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name or device name is null or empty.</exception>
        public Task<bool> DeleteDeviceAsync(string channelName, string deviceName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(channelName))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(channelName));
            if (string.IsNullOrEmpty(deviceName))
                throw new ArgumentException("Device name cannot be null or empty", nameof(deviceName));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync<Device>([deviceName, channelName], cancellationToken: cancellationToken);
        }
        #endregion
        #endregion

        #region recursive methods
        private static void SetOwnerRecursive(IEnumerable<DeviceTagGroup> tagGroups, NamedEntity owner)
        {
            foreach (var tagGroup in tagGroups)
            {
                tagGroup.Owner = owner;

                if (tagGroup.Tags != null)
                    foreach (var tag in tagGroup.Tags)
                        tag.Owner = tagGroup;

                if (tagGroup.TagGroups != null)
                    SetOwnerRecursive(tagGroup.TagGroups, tagGroup);
            }
        }

        /// <summary>
        /// Recursively compares and applies changes to tag groups.
        /// </summary>
        /// <param name="left">The left tag group collection to compare.</param>
        /// <param name="right">The right tag group collection to compare.</param>
        /// <param name="owner">The owner of the tag groups.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the counts of inserts, updates, and deletes.</returns>
        private async Task<(int inserts, int updates, int deletes)> RecusivlyCompareTagGroup(DeviceTagGroupCollection left, DeviceTagGroupCollection? right, NamedEntity owner, CancellationToken cancellationToken)
        {
            (int inserts, int updates, int deletes) ret = (0, 0, 0);

            var tagGroupCompare = await m_kepwareApiClient.GenericConfig.CompareAndApply<DeviceTagGroupCollection, DeviceTagGroup>(left, right, owner, cancellationToken: cancellationToken).ConfigureAwait(false);

            ret.inserts = tagGroupCompare.ItemsOnlyInLeft.Count;
            ret.updates = tagGroupCompare.ChangedItems.Count;
            ret.deletes = tagGroupCompare.ItemsOnlyInRight.Count;

            foreach (var tagGroup in tagGroupCompare.UnchangedItems.Concat(tagGroupCompare.ChangedItems))
            {
                var tagGroupTagCompare = await m_kepwareApiClient.GenericConfig.CompareAndApply<DeviceTagGroupTagCollection, Tag>(tagGroup.Left!.Tags, tagGroup.Right!.Tags, tagGroup.Right, cancellationToken: cancellationToken).ConfigureAwait(false);

                ret.inserts = tagGroupTagCompare.ItemsOnlyInLeft.Count;
                ret.updates = tagGroupTagCompare.ChangedItems.Count;
                ret.deletes = tagGroupTagCompare.ItemsOnlyInRight.Count;

                if (tagGroup.Left!.TagGroups != null)
                {
                    var result = await RecusivlyCompareTagGroup(tagGroup.Left!.TagGroups, tagGroup.Right!.TagGroups, tagGroup.Right, cancellationToken: cancellationToken).ConfigureAwait(false);
                    ret.updates += result.updates;
                    ret.deletes += result.deletes;
                    ret.inserts += result.inserts;
                }
            }

            return ret;
        }

        /// <summary>
        /// Recursively loads tag groups and their tags.
        /// </summary>
        /// <param name="tagGroups">The tag groups to load.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        internal async Task LoadTagGroupsRecursiveAsync(IEnumerable<DeviceTagGroup> tagGroups, CancellationToken cancellationToken = default)
        {
            foreach (var tagGroup in tagGroups)
            {
                // Lade die TagGroups der aktuellen TagGroup
                tagGroup.TagGroups = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(tagGroup, cancellationToken).ConfigureAwait(false);
                tagGroup.Tags = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupTagCollection, Tag>(tagGroup, cancellationToken).ConfigureAwait(false);

                // Rekursiver Aufruf für die geladenen TagGroups
                if (tagGroup.TagGroups != null && tagGroup.TagGroups.Count > 0)
                {
                    await LoadTagGroupsRecursiveAsync(tagGroup.TagGroups, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        #endregion
    }
}
