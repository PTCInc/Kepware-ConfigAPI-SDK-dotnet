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
    /// Handles operations related to projects in the Kepware server.
    /// </summary>
    public class ProjectApiHandler
    {
        private const string ENDPONT_FULL_PROJECT = "/config/v1/project?content=serialize";

        private readonly KepwareApiClient m_kepwareApiClient;
        private readonly ILogger<ProjectApiHandler> m_logger;

        public ChannelApiHandler Channels { get; }
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
                m_logger.LogInformation("Project properties has changed. Updating project properties...");
                var result = await SetProjectPropertiesAsync(projectFromApi, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!result)
                {
                    m_logger.LogError("Failed to update project properties...");
                }
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

        #region ProjectProperties

        /// <summary>
        /// Gets the project properties from the Kepware server.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
        /// <returns></returns>
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
                    }

                    m_logger.LogInformation("Loaded project in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
                }

                return project;
            }
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
                var tagGroupTagCompare = await apiClient.GenericConfig.CompareAndApply<DeviceTagGroupTagCollection, Tag>(tagGroup.Left!.Tags, tagGroup.Right!.Tags, tagGroup.Right, cancellationToken: cancellationToken).ConfigureAwait(false);

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
                // Lade die TagGroups der aktuellen TagGroup
                tagGroup.TagGroups = await apiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(tagGroup, cancellationToken).ConfigureAwait(false);
                tagGroup.Tags = await apiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupTagCollection, Tag>(tagGroup, cancellationToken).ConfigureAwait(false);

                // Rekursiver Aufruf für die geladenen TagGroups
                if (tagGroup.TagGroups != null && tagGroup.TagGroups.Count > 0)
                {
                    await LoadTagGroupsRecursiveAsync(apiClient, tagGroup.TagGroups, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        #endregion
    }
}
