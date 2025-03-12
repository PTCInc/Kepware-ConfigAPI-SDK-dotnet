using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kepware.Api
{
    public partial class KepwareApiClient
    {
        #region ProjectProperties

        /// <summary>
        /// Gets the project properties from the Kepware server.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Project?> GetProjectPropertiesAsync(CancellationToken cancellationToken = default)
        {
            var project = await LoadEntityAsync<Project>(name: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return project;

        }

        /// <summary>
        /// Sets the project properties on the Kepware server.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<bool> SetProjectPropertiesAsync(Project project, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentProject = await LoadEntityAsync<Project>(name: null, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (currentProject == null)
                {
                    throw new InvalidOperationException("Failed to retrieve current settings");
                }

                var endpoint = EndpointResolver.ResolveEndpoint<Project>();
                var diff = project.GetUpdateDiff(currentProject);

                // Need to ensure ProjectId is captured. GetUpdateDiff doesn't return ProjectId on non_NamedEntity types
                diff.Add(Properties.ProjectId, KepJsonContext.WrapInJsonElement(currentProject.ProjectId));
                
                m_logger.LogInformation("Updating Project Property Settings on {Endpoint}, values {Diff}", endpoint, diff);

                HttpContent httpContent = new StringContent(
                         JsonSerializer.Serialize(diff, KepJsonContext.Default.DictionaryStringJsonElement),
                         Encoding.UTF8,
                         "application/json"
                     );

                var response = await m_httpClient.PutAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    m_logger.LogError("Failed to update Project Property Settings from {Endpoint}: {ReasonPhrase}\n{Message}", endpoint, response.ReasonPhrase, message);
                }
                else
                {
                    return true;
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_blnIsConnected = null;
            }

            return false;
        }

        #endregion

        #region LoadProject
        /// <summary>
        /// Loads the project from the Kepware server.
        /// </summary>
        /// <param name="blnLoadFullProject"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Project> LoadProject(bool blnLoadFullProject = false, CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var productInfo = await GetProductInfoAsync(cancellationToken).ConfigureAwait(false);

            if (blnLoadFullProject && productInfo?.SupportsJsonProjectLoadService == true)
            {
                try
                {
                    var response = await m_httpClient.GetAsync(ENDPONT_FULL_PROJECT, cancellationToken).ConfigureAwait(false);
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
                    m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                    m_blnIsConnected = null;
                }

                m_logger.LogWarning("Failed to load project");
                return new Project();
            }
            else
            {
                var project = await LoadEntityAsync<Project>(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (project == null)
                {
                    m_logger.LogWarning("Failed to load project");
                    project = new Project();
                }
                else if (blnLoadFullProject)
                {
                    project.Channels = await LoadCollectionAsync<ChannelCollection, Channel>(cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (project.Channels != null)
                    {
                        int totalChannelCount = project.Channels.Count;
                        int loadedChannelCount = 0;
                        await Task.WhenAll(project.Channels.Select(async channel =>
                        {
                            channel.Devices = await LoadCollectionAsync<DeviceCollection, Device>(channel, cancellationToken).ConfigureAwait(false);

                            if (channel.Devices != null)
                            {
                                await Task.WhenAll(channel.Devices.Select(async device =>
                                {
                                    device.Tags = await LoadCollectionAsync<DeviceTagCollection, Tag>(device, cancellationToken: cancellationToken).ConfigureAwait(false);
                                    device.TagGroups = await LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device, cancellationToken: cancellationToken).ConfigureAwait(false);

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
        /// <summary>
        /// Gets or creates a channel with the specified name and driver.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="driverName"></param>
        /// <param name="properties"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Channel> GetOrCreateChannelAsync(string name, string driverName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(driverName))
                throw new ArgumentException("Driver name cannot be null or empty", nameof(driverName));

            var channel = await LoadEntityAsync<Channel>(name, cancellationToken: cancellationToken);

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
                            await UpdateItemAsync(channel, cancellationToken: cancellationToken);
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

            if (await InsertItemAsync<ChannelCollection, Channel>(channel, cancellationToken: cancellationToken))
            {
                return channel;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Devices
        public async Task<Device> GetOrCreateDeviceAsync(Channel channel, string name, string? driverName = default, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Device name cannot be null or empty", nameof(name));

            var device = await LoadEntityAsync<Device>(name, channel, cancellationToken: cancellationToken);

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
                            await UpdateItemAsync(device, cancellationToken: cancellationToken);
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
                device.Tags = await LoadCollectionAsync<DeviceTagCollection, Tag>(device, cancellationToken: cancellationToken).ConfigureAwait(false);
                device.TagGroups = await LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (device.TagGroups != null)
                {
                    await LoadTagGroupsRecursiveAsync(device.TagGroups, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            return device;
        }

        public async Task<Device?> CreateDeviceAsync(Channel channel, string name, string? driverName = default, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Device name cannot be null or empty", nameof(name));

            var device = new Device { Name = name, Owner = channel };

            driverName = driverName ?? channel.GetDynamicProperty<string>(Properties.Channel.DeviceDriver);
            device.SetDynamicProperty(Properties.Channel.DeviceDriver, driverName);

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    device.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await InsertItemAsync<DeviceCollection, Device>(device, channel, cancellationToken: cancellationToken))
            {
                return device;
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}
