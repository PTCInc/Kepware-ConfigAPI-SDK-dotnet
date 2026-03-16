using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kepware.Api.ClientHandler
{
    /// <summary>
    /// Handles operations related to projects and project properties in the Kepware server.
    /// </summary>
    public class ProjectApiHandler
    {
        private const string ENDPONT_FULL_PROJECT = "/config/v1/project?content=serialize";

        private readonly KepwareApiClient m_kepwareApiClient;
        private readonly ILogger<ProjectApiHandler> m_logger;

        /// <summary>
        /// Gets the channel handlers.
        /// </summary>
        /// <remarks> See <see cref="Kepware.Api.ClientHandler.ChannelApiHandler"/> for method references.</remarks>
        public ChannelApiHandler Channels { get; }

        /// <summary>
        /// Gets the device handlers.
        /// </summary>
        /// <remarks> See <see cref="Kepware.Api.ClientHandler.DeviceApiHandler"/> for method references.</remarks>
        public DeviceApiHandler Devices { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectApiHandler"/> class.
        /// </summary>
        /// <param name="kepwareApiClient">The Kepware API client.</param>
        /// <param name="channelApiHandler">The channel API handler.</param>
        /// <param name="deviceApiHandler">The device API handler.</param>
        /// <param name="logger">The logger instance.</param>
        public ProjectApiHandler(KepwareApiClient kepwareApiClient, ChannelApiHandler channelApiHandler, DeviceApiHandler deviceApiHandler, ILogger<ProjectApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_logger = logger;

            Channels = channelApiHandler;
            Devices = deviceApiHandler;
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
            var projectFromApi = await LoadProjectAsync(blnLoadFullProject: true, cancellationToken: cancellationToken);
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
            int inserts = 0, updates = 0, deletes = 0;

            if (sourceProject.Hash != projectFromApi.Hash)
            {
                m_logger.LogInformation("Project properties has changed. Updating project properties...");
                var result = await SetProjectPropertiesAsync(sourceProject, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result)
                {
                    updates += 1;
                }
                else
                {
                    m_logger.LogError("Failed to update project properties...");
                }
            }
            

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
                        var tagGroupTagCompare = await m_kepwareApiClient.GenericConfig.CompareAndApply<DeviceTagCollection, Tag>(tagGroup.Left!.Tags, tagGroup.Right!.Tags, tagGroup.Right, cancellationToken).ConfigureAwait(false);

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

        #region ProjectProperties

        /// <summary>
        /// Gets the project properties from the Kepware server.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="Project"/> or null if retrieval fails.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Project?> GetProjectPropertiesAsync(CancellationToken cancellationToken = default)
        {
            var project = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Project>(name: null, cancellationToken: cancellationToken).ConfigureAwait(false);
            return project;

        }

        /// <summary>
        /// Sets the project properties on the Kepware server.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the update was successful.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<bool> SetProjectPropertiesAsync(Project project, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentProject = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Project>(name: null, cancellationToken: cancellationToken).ConfigureAwait(false);

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

                var response = await m_kepwareApiClient.HttpClient.PutAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);

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
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_kepwareApiClient.HttpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }

            return false;
        }

        #endregion

        #region LoadProject

        /// <summary>
        /// Does the same as <see cref="LoadProjectAsync(bool, CancellationToken)"/> but is marked as obsolete.
        /// </summary>
        /// <param name="blnLoadFullProject">Indicates whether to load the full project.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="Project"/>.</returns>
        /// <remarks>Deprecated in v1.1.0; will be removed in future release</remarks>
        [Obsolete("Use LoadProjectAsync() instead. This will be removed in future release", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public async Task<Project> LoadProject(bool blnLoadFullProject = false, CancellationToken cancellationToken = default)
        {
            return await LoadProjectAsync(blnLoadFullProject, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads the project from the Kepware server. If <paramref name="blnLoadFullProject"/> is true, it loads the full project, otherwise only
        /// the project properties will be returned. 
        /// </summary>
        /// <param name="blnLoadFullProject">Indicates whether to load the full project.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="Project"/>.</returns>
        /// <remarks>NOTE: When loading a full project, the project will be loaded either via the JsonProjectLoad service,  an "optimized"
        /// recursion that uses the content=serialize query or a basic recurion through project tree. </remarks>
        public async Task<Project> LoadProjectAsync(bool blnLoadFullProject = false, CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var project = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Project>(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (project == null)
            {
                m_logger.LogWarning("Failed to load project");
                project = new Project();
            }

            // If not loading full project, return with just project properties.
            if (!blnLoadFullProject)
            {
                m_logger.LogInformation("Loaded project properties in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                return project;

            }


            var productInfo = await m_kepwareApiClient.GetProductInfoAsync(cancellationToken).ConfigureAwait(false);

            if (productInfo?.SupportsJsonProjectLoadService == true)
            {
                try
                {
                    // Optimized recursive loading approach that uses the content=serialize query parameter.
                    // This approach significantly reduces the number of API calls when loading projects with a large number of tags and prevents 
                    // timeout errors with large projects.

                    // TODO: change threshold to configurable option
                    // Currently hardcoded to 100000 tags as the threshold to use content=serialize based loading or full recursive loading.
                    var tagLimit = 100000;
                    if (int.TryParse(project.GetDynamicProperty<string>(Properties.ProjectSettings.TagsDefined), out int count) && count > tagLimit)
                    {
                        m_logger.LogInformation("Project has greater than {TagLimit} tags defined. Loading project via optimized recursion...", tagLimit);
                        
                        project = await LoadProjectOptimizedRecurisveAsync(project, tagLimit, cancellationToken).ConfigureAwait(false);

                        if (!project.IsEmpty)
                        {
                            SetOwnersFullProject(project);
                            m_logger.LogInformation("Loaded project via optimized recursion in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                        }
                        return project;
                    }

                    // If project has less than tagLimit number of tags, load full project via JsonProjectLoad service.
                    else
                    {
                        m_logger.LogInformation("Project has less than {TagLimit} tags defined. Loading project via JsonProjectLoad Service...", tagLimit);
                        var response = await m_kepwareApiClient.HttpClient.GetAsync(ENDPONT_FULL_PROJECT, cancellationToken).ConfigureAwait(false);
                        if (response.IsSuccessStatusCode)
                        {
                            var prjRoot = await JsonSerializer.DeserializeAsync(
                                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                                KepJsonContext.Default.JsonProjectRoot, cancellationToken).ConfigureAwait(false);

                            // Set the Owner property for all loaded entities.
                            if (prjRoot?.Project != null)
                            {
                                prjRoot.Project.IsLoadedByProjectLoadService = true;

                                SetOwnersFullProject(prjRoot.Project);

                                m_logger.LogInformation("Loaded project via JsonProjectLoad Service in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                                project = prjRoot.Project;
                            }
                            else
                            {
                                m_logger.LogWarning("Failed to deserialize project loaded via JsonProjectLoad Service");
                                project = new Project();
                            }
                        }
                        return project;
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
                project = await LoadProjectRecursiveAsync(project, cancellationToken).ConfigureAwait(false);

                if (!project.IsEmpty)
                {
                    m_logger.LogInformation("Loaded project in via non-optimized recursion {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                }

                return project;
            }
        }

        private static void SetOwnersFullProject(Project project)
        {
            if (project.Channels != null)
                foreach (var channel in project.Channels)
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
        }

        private async Task<Project> LoadProjectOptimizedRecurisveAsync(Project project, int tagLimit, CancellationToken cancellationToken = default)
        {
            project.Channels = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<ChannelCollection, Channel>(cancellationToken: cancellationToken);
            if (project.Channels != null)
            {
                int totalChannelCount = project.Channels.Count;
                int loadedChannelCount = 0;
                await Task.WhenAll(project.Channels.Select(async (channel, c_index) =>
                {

                    if (channel.GetDynamicProperty<int>(Properties.Channel.StaticTagCount) < tagLimit)
                    {
                        var query = new[]
                        {
                                        new KeyValuePair<string, string?>("content", "serialize")
                                    };
                        var loadedChannel = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>(channel.Name, query, cancellationToken: cancellationToken);
                        if (loadedChannel != null)
                        {
                            project.Channels[c_index] = loadedChannel;
                        }
                        else
                        {
                            // Failed to load channel, log warning and end without incrementing completion.
                            m_logger.LogWarning("Failed to load {ChannelName}", channel.Name);
                            return;
                        }
                    }
                    else
                    {
                        channel.Devices = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceCollection, Device>(channel, cancellationToken: cancellationToken).ConfigureAwait(false);

                        if (channel.Devices != null)
                        {
                            await Task.WhenAll(channel.Devices.Select(async (device, d_index) =>
                            {
                                if (device.GetDynamicProperty<int>(Properties.Device.StaticTagCount) < tagLimit)
                                {
                                    var query = new[]
                                    {
                                                    new KeyValuePair<string, string?>("content", "serialize")
                                                };
                                    var loadedDevice = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Device>(device.Name, channel, query, cancellationToken: cancellationToken).ConfigureAwait(false);
                                    if (loadedDevice != null)
                                    {
                                        project.Channels[c_index].Devices![d_index] = loadedDevice;
                                    }
                                    else
                                    {
                                        // Failed to load device, log warning and end without incrementing completion.
                                        m_logger.LogWarning("Failed to load {DeviceName} in channel {ChannelName}", device.Name, channel.Name);
                                        return;
                                    }
                                }
                                else
                                {
                                    device.Tags = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(device, cancellationToken: cancellationToken).ConfigureAwait(false);
                                    device.TagGroups = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device, cancellationToken: cancellationToken).ConfigureAwait(false);

                                    if (device.TagGroups != null)
                                    {
                                        await LoadTagGroupsRecursiveAsync(m_kepwareApiClient, device.TagGroups, cancellationToken: cancellationToken).ConfigureAwait(false);
                                    }
                                }

                            }));
                        }

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

                // If loaded channel count doesn't match total channel count, log warning that some channels may have failed to load.
                // Return empty project to avoid returning a partially loaded project which may cause issues for consumers of the API.
                if (loadedChannelCount != totalChannelCount)
                {
                    m_logger.LogWarning("Only loaded {LoadedChannelCount} of {TotalChannelCount} channels. Some channels may have fail to load.", loadedChannelCount, totalChannelCount);
                    project = new Project();
                }
            }

            return project;
        }

        private async Task<Project> LoadProjectRecursiveAsync(Project project, CancellationToken cancellationToken = default)
        {
            project.Channels = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<ChannelCollection, Channel>(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (project.Channels != null)
            {
                int totalChannelCount = project.Channels.Count;
                int loadedChannelCount = 0;
                await Task.WhenAll(project.Channels.Select(async channel =>
                {
                    channel.Devices = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceCollection, Device>(channel, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (channel.Devices != null)
                    {
                        await Task.WhenAll(channel.Devices.Select(async device =>
                        {
                            device.Tags = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(device, cancellationToken: cancellationToken).ConfigureAwait(false);
                            device.TagGroups = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device, cancellationToken: cancellationToken).ConfigureAwait(false);

                            if (device.TagGroups != null)
                            {
                                await LoadTagGroupsRecursiveAsync(m_kepwareApiClient, device.TagGroups, cancellationToken: cancellationToken).ConfigureAwait(false);
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
                // If loaded channel count doesn't match total channel count, log warning that some channels may have failed to load.
                // Return empty project to avoid returning a partially loaded project which may cause issues for consumers of the API.
                if (loadedChannelCount != totalChannelCount)
                {
                    m_logger.LogWarning("Only loaded {LoadedChannelCount} of {TotalChannelCount} channels. Some channels may have fail to load.", loadedChannelCount, totalChannelCount);
                    project = new Project();
                }
            }
            return project;
        }
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
        private Task<(int inserts, int updates, int deletes)> RecusivlyCompareTagGroup(DeviceTagGroupCollection left, DeviceTagGroupCollection? right, NamedEntity owner, CancellationToken cancellationToken)
         => RecusivlyCompareTagGroup(m_kepwareApiClient, left, right, owner, cancellationToken);

        /// <summary>
        /// Recursively compares and applies changes to tag groups.
        /// </summary>
        /// <param name="apiClient">The API client.</param>
        /// <param name="left">The left tag group collection to compare.</param>
        /// <param name="right">The right tag group collection to compare.</param>
        /// <param name="owner">The owner of the tag groups.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the counts of inserts, updates, and deletes.</returns>
        internal static async Task<(int inserts, int updates, int deletes)> RecusivlyCompareTagGroup(KepwareApiClient apiClient, DeviceTagGroupCollection left, DeviceTagGroupCollection? right, NamedEntity owner, CancellationToken cancellationToken)
        {
            (int inserts, int updates, int deletes) ret = (0, 0, 0);

            var tagGroupCompare = await apiClient.GenericConfig.CompareAndApply<DeviceTagGroupCollection, DeviceTagGroup>(left, right, owner, cancellationToken: cancellationToken).ConfigureAwait(false);

            ret.inserts = tagGroupCompare.ItemsOnlyInLeft.Count;
            ret.updates = tagGroupCompare.ChangedItems.Count;
            ret.deletes = tagGroupCompare.ItemsOnlyInRight.Count;

            foreach (var tagGroup in tagGroupCompare.UnchangedItems.Concat(tagGroupCompare.ChangedItems))
            {
                var tagGroupTagCompare = await apiClient.GenericConfig.CompareAndApply<DeviceTagCollection, Tag>(tagGroup.Left!.Tags, tagGroup.Right!.Tags, tagGroup.Right, cancellationToken: cancellationToken).ConfigureAwait(false);

                ret.inserts = tagGroupTagCompare.ItemsOnlyInLeft.Count;
                ret.updates = tagGroupTagCompare.ChangedItems.Count;
                ret.deletes = tagGroupTagCompare.ItemsOnlyInRight.Count;

                if (tagGroup.Left!.TagGroups != null)
                {
                    var result = await RecusivlyCompareTagGroup(apiClient, tagGroup.Left!.TagGroups, tagGroup.Right!.TagGroups, tagGroup.Right, cancellationToken: cancellationToken).ConfigureAwait(false);
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
        /// <param name="apiClient">The API client.</param>
        /// <param name="tagGroups">The tag groups to load.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        internal static async Task LoadTagGroupsRecursiveAsync(KepwareApiClient apiClient, IEnumerable<DeviceTagGroup> tagGroups, CancellationToken cancellationToken = default)
        {
            foreach (var tagGroup in tagGroups)
            {
                // Load the Tag Groups and Tags of the current Tag Group
                tagGroup.TagGroups = await apiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(tagGroup, cancellationToken: cancellationToken).ConfigureAwait(false);
                tagGroup.Tags = await apiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(tagGroup, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Recursively load the Tag Groups and Tags of the child Tag Groups
                if (tagGroup.TagGroups != null && tagGroup.TagGroups.Count > 0)
                {
                    await LoadTagGroupsRecursiveAsync(apiClient, tagGroup.TagGroups, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        #endregion
    }
}
