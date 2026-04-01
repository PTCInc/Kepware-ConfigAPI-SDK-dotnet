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
        /// Gets the IoT Gateway handlers.
        /// </summary>
        /// <remarks> See <see cref="Kepware.Api.ClientHandler.IotGatewayApiHandler"/> for method references.</remarks>
        public IotGatewayApiHandler IotGateway { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectApiHandler"/> class.
        /// </summary>
        /// <param name="kepwareApiClient">The Kepware API client.</param>
        /// <param name="channelApiHandler">The channel API handler.</param>
        /// <param name="deviceApiHandler">The device API handler.</param>
        /// <param name="iotGatewayApiHandler">The IoT Gateway API handler.</param>
        /// <param name="logger">The logger instance.</param>
        public ProjectApiHandler(KepwareApiClient kepwareApiClient, ChannelApiHandler channelApiHandler, DeviceApiHandler deviceApiHandler, IotGatewayApiHandler iotGatewayApiHandler, ILogger<ProjectApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_logger = logger;

            Channels = channelApiHandler;
            Devices = deviceApiHandler;
            IotGateway = iotGatewayApiHandler;
        }

        #region CompareAndApply
        /// <summary>
        /// Compares the source project with the project from the API and applies the changes to the API.
        /// </summary>
        /// <param name="sourceProject">The source project to compare.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the counts of inserts, updates, and deletes.</returns>
        public async Task<(int inserts, int updates, int deletes)> CompareAndApplyAsync(Project sourceProject, CancellationToken cancellationToken = default)
        {
            var result = await CompareAndApplyDetailedAsync(sourceProject, cancellationToken).ConfigureAwait(false);
            return (result.Inserts, result.Updates, result.Deletes);
        }

        /// <summary>
        /// Compares the source project with the project from the API and applies the changes to the API.
        /// </summary>
        /// <param name="sourceProject">The source project to compare.</param>
        /// <param name="projectFromApi">The project loaded from the API.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the counts of inserts, updates, and deletes.</returns>
        public async Task<(int inserts, int updates, int deletes)> CompareAndApplyAsync(Project sourceProject, Project projectFromApi, CancellationToken cancellationToken = default)
        {
            var result = await CompareAndApplyDetailedAsync(sourceProject, projectFromApi, cancellationToken).ConfigureAwait(false);
            return (result.Inserts, result.Updates, result.Deletes);
        }

        /// <summary>
        /// Compares the source project with the project from the API and applies changes while returning detailed success and failure information.
        /// </summary>
        /// <param name="sourceProject">The source project to compare.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="ProjectCompareAndApplyResult"/> including counts and failed items.</returns>
        public async Task<ProjectCompareAndApplyResult> CompareAndApplyDetailedAsync(Project sourceProject, CancellationToken cancellationToken = default)
        {
            var projectFromApi = await LoadProjectAsync(blnLoadFullProject: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            await projectFromApi.Cleanup(m_kepwareApiClient, true, cancellationToken).ConfigureAwait(false);
            return await CompareAndApplyDetailedAsync(sourceProject, projectFromApi, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Compares the source project with the project from the API and applies changes while returning detailed success and failure information.
        /// </summary>
        /// <param name="sourceProject">The source project to compare.</param>
        /// <param name="projectFromApi">The project loaded from the API.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="ProjectCompareAndApplyResult"/> including counts and failed items.</returns>
        public async Task<ProjectCompareAndApplyResult> CompareAndApplyDetailedAsync(Project sourceProject, Project projectFromApi, CancellationToken cancellationToken = default)
        {
            var result = new ProjectCompareAndApplyResult();

            if (sourceProject.Hash != projectFromApi.Hash)
            {
                m_logger.LogInformation("Project properties has changed. Updating project properties...");
                var projectPropertyFailure = await SetProjectPropertiesDetailedAsync(sourceProject, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (projectPropertyFailure == null)
                {
                    result.AddUpdateSuccess();
                }
                else
                {
                    m_logger.LogError("Failed to update project properties...");
                    result.AddFailure(projectPropertyFailure);
                }
            }

            var channelCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<ChannelCollection, Channel>(sourceProject.Channels, projectFromApi.Channels,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            result.Add(channelCompare);

            foreach (var channel in channelCompare.CompareResult.UnchangedItems.Concat(channelCompare.CompareResult.ChangedItems))
            {
                var deviceCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<DeviceCollection, Device>(channel.Left!.Devices, channel.Right!.Devices, channel.Right,
                cancellationToken: cancellationToken).ConfigureAwait(false);
                result.Add(deviceCompare);

                foreach (var device in deviceCompare.CompareResult.UnchangedItems.Concat(deviceCompare.CompareResult.ChangedItems))
                {
                    var tagCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<DeviceTagCollection, Tag>(device.Left!.Tags, device.Right!.Tags, device.Right, cancellationToken).ConfigureAwait(false);
                    result.Add(tagCompare);

                    var tagGroupCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<DeviceTagGroupCollection, DeviceTagGroup>(device.Left!.TagGroups, device.Right!.TagGroups, device.Right, cancellationToken).ConfigureAwait(false);
                    result.Add(tagGroupCompare);

                    foreach (var tagGroup in tagGroupCompare.CompareResult.UnchangedItems.Concat(tagGroupCompare.CompareResult.ChangedItems))
                    {
                        var tagGroupTagCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<DeviceTagCollection, Tag>(tagGroup.Left!.Tags, tagGroup.Right!.Tags, tagGroup.Right, cancellationToken).ConfigureAwait(false);
                        result.Add(tagGroupTagCompare);

                        if (tagGroup.Left?.TagGroups != null)
                        {
                            var recursiveResult = await RecusivlyCompareTagGroupDetailed(tagGroup.Left!.TagGroups, tagGroup.Right!.TagGroups, tagGroup.Right, cancellationToken).ConfigureAwait(false);
                            result.Add(recursiveResult);
                        }
                    }
                }
            }

            // Compare and apply IoT Gateway agents and their IoT Items
            await CompareAndApplyIotGatewayDetailedAsync(sourceProject.IotGateway, projectFromApi.IotGateway, result, cancellationToken).ConfigureAwait(false);

            return result;
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
            return await SetProjectPropertiesDetailedAsync(project, cancellationToken).ConfigureAwait(false) == null;
        }

        private async Task<ApplyFailure?> SetProjectPropertiesDetailedAsync(Project project, CancellationToken cancellationToken = default)
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
                    return new ApplyFailure
                    {
                        Operation = ApplyOperation.Update,
                        AttemptedItem = project,
                        ResponseCode = (int)response.StatusCode,
                        ResponseMessage = message,
                    };
                }
                else
                {
                    var updateMessage = await TryDeserializeUpdateMessageAsync(response, cancellationToken).ConfigureAwait(false);
                    if (updateMessage?.NotApplied != null && updateMessage.NotApplied.Count > 0)
                    {
                        var notApplied = updateMessage.NotApplied.Keys.ToList();
                        m_logger.LogError("Partial update detected for project properties on {Endpoint}. Not applied properties: {NotApplied}", endpoint, notApplied);
                        return new ApplyFailure
                        {
                            Operation = ApplyOperation.Update,
                            AttemptedItem = project,
                            ResponseCode = updateMessage.ResponseStatusCode,
                            ResponseMessage = updateMessage.Message,
                            NotAppliedProperties = notApplied,
                        };
                    }

                    return null;
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_kepwareApiClient.HttpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }

            return new ApplyFailure
            {
                Operation = ApplyOperation.Update,
                AttemptedItem = project,
            };
        }

        private static async Task<UpdateApiResponseMessage?> TryDeserializeUpdateMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize(body, KepJsonContext.Default.UpdateApiResponseMessage);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        #endregion

        #region LoadProject

        /// <summary>
        /// Does the same as <see cref="LoadProjectAsync(bool, int, CancellationToken)"/> but is marked as obsolete.
        /// </summary>
        /// <param name="blnLoadFullProject">Indicates whether to load the full project.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="Project"/>.</returns>
        /// <remarks>Deprecated in v1.1.0; will be removed in future release</remarks>
        [Obsolete("Use LoadProjectAsync() instead. This will be removed in future release", false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public async Task<Project> LoadProject(bool blnLoadFullProject = false, CancellationToken cancellationToken = default)
        {
            return await LoadProjectAsync(blnLoadFullProject, m_kepwareApiClient.ClientOptions.ProjectLoadTagLimit ,cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads the project from the Kepware server. If <paramref name="blnLoadFullProject"/> is true, it loads the full project, otherwise only
        /// the project properties will be returned. 
        /// </summary>
        /// <param name="blnLoadFullProject">Indicates whether to load the full project.</param>
        /// <param name="projectLoadTagLimit">The tag count threshold to determine whether to use optimized content=serialize 
        /// loading or basic recursive loading when loading the full project. This is only applicable for projects loaded with 
        /// the full project load option and when the JsonProjectLoad service is supported by the server.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="Project"/>.</returns>
        /// <remarks>NOTE: When loading a full project, the project will be loaded either via the JsonProjectLoad service,  an "optimized"
        /// recursion that uses the content=serialize query or a basic recurion through project tree. Putting a value for <paramref name="projectLoadTagLimit"/>
        /// will override the value set in <see cref="KepwareApiClientOptions.ProjectLoadTagLimit"/> during initial client creation.</remarks>
        public async Task<Project> LoadProjectAsync(bool blnLoadFullProject = false, int projectLoadTagLimit = 0,  CancellationToken cancellationToken = default)
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
                return project;
            }


            var productInfo = await m_kepwareApiClient.GetProductInfoAsync(cancellationToken).ConfigureAwait(false);

            if (productInfo?.SupportsJsonProjectLoadService == true)
            {
                // Check to see if projectLoadTagLimit parameter is set on call. If not or an invalid value, use value ]
                // set by <see cref="KepwareApiClientOptions.ProjectLoadTagLimit"/> as the threshold to use content=serialize
                // based loading or full recursive loading.
                if (projectLoadTagLimit <= 0)
                    projectLoadTagLimit = m_kepwareApiClient.ClientOptions.ProjectLoadTagLimit;

                try
                {

                    // Optimized recursive loading approach that uses the content=serialize query parameter.
                    // This approach significantly reduces the number of API calls when loading projects with a large number of tags and prevents 
                    // timeout errors with large projects.
                    
                    if (int.TryParse(project.GetDynamicProperty<string>(Properties.ProjectSettings.TagsDefined), out int count) && count > projectLoadTagLimit)
                    {
                        m_logger.LogInformation("Project has greater than {TagLimit} tags defined. Loading project via optimized recursion...", projectLoadTagLimit);
                        
                        project = await LoadProjectOptimizedRecurisveAsync(project, projectLoadTagLimit, cancellationToken).ConfigureAwait(false);

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
                        m_logger.LogInformation("Project has less than {TagLimit} tags defined. Loading project via JsonProjectLoad Service...", projectLoadTagLimit);
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

            if (project.IotGateway != null)
            {
                foreach (var agent in (project.IotGateway.MqttClientAgents ?? []).Cast<IotAgent>()
                    .Concat(project.IotGateway.RestClientAgents ?? [])
                    .Concat(project.IotGateway.RestServerAgents ?? []))
                {
                    if (agent.IotItems != null)
                        foreach (var item in agent.IotItems)
                            item.Owner = agent;
                }
            }
        }

        private async Task<Project> LoadProjectOptimizedRecurisveAsync(Project project, int tagLimit, CancellationToken cancellationToken = default)
        {
            project.Channels = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<ChannelCollection, Channel>(cancellationToken: cancellationToken);
            if (project.Channels != null)
            {
                var query = new[]
                {
                    new KeyValuePair<string, string?>("content", "serialize")
                };
                int totalChannelCount = project.Channels.Count;
                int loadedChannelCount = 0;

                // Create a list of tasks by iterating indices to avoid modifying the collection while it's being enumerated.
                var channelTasks = new List<Task>();
                for (int c_index = 0; c_index < project.Channels.Count; c_index++)
                {
                    int channelIndex = c_index;
                    var channel = project.Channels[channelIndex];
                    channelTasks.Add(Task.Run(async () =>
                    {
                        if (channel.GetDynamicProperty<int>(Properties.Channel.StaticTagCount) < tagLimit)
                        {
                            var loadedChannel = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>(channel.Name, query, cancellationToken: cancellationToken).ConfigureAwait(false);
                            if (loadedChannel != null)
                            {
                                project.Channels[channelIndex] = loadedChannel;
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
                                    var deviceTasks = new List<Task>();
                                    for (int d_index = 0; d_index < channel.Devices.Count; d_index++)
                                    {
                                        int deviceIndex = d_index;
                                        var device = channel.Devices[deviceIndex];
                                        deviceTasks.Add(Task.Run(async () =>
                                        {
                                            if (device.GetDynamicProperty<int>(Properties.Device.StaticTagCount) < tagLimit)
                                            {
                                                var loadedDevice = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Device>(device.Name, channel, query, cancellationToken: cancellationToken).ConfigureAwait(false);
                                                if (loadedDevice != null)
                                                {
                                                    project.Channels[channelIndex].Devices![deviceIndex] = loadedDevice;
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
                                                    await LoadTagGroupsRecursiveAsync(m_kepwareApiClient, device.TagGroups, optimizedRecursion: true, tagLimit: tagLimit, cancellationToken: cancellationToken).ConfigureAwait(false);
                                                }
                                            }
                                        }));
                                    }
                                    await Task.WhenAll(deviceTasks).ConfigureAwait(false);
                                }
                        }

                        // Log information, loaded channel <Name> x of y
                        System.Threading.Interlocked.Increment(ref loadedChannelCount);
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

                await Task.WhenAll(channelTasks).ConfigureAwait(false);

                // If loaded channel count doesn't match total channel count, log warning that some channels may have failed to load.
                // Return empty project to avoid returning a partially loaded project which may cause issues for consumers of the API.
                if (loadedChannelCount != totalChannelCount)
                {
                    m_logger.LogWarning("Only loaded {LoadedChannelCount} of {TotalChannelCount} channels. Some channels may have fail to load.", loadedChannelCount, totalChannelCount);
                    project = new Project();
                }
            }

            // Load IoT Gateway agents and their IoT Items
            await LoadIotGatewayRecursiveAsync(project, cancellationToken).ConfigureAwait(false);

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

            // Load IoT Gateway agents and their IoT Items
            await LoadIotGatewayRecursiveAsync(project, cancellationToken).ConfigureAwait(false);

            return project;
        }

        private async Task LoadIotGatewayRecursiveAsync(Project project, CancellationToken cancellationToken)
        {
            MqttClientAgentCollection? mqttClients;
            RestClientAgentCollection? restClients;
            RestServerAgentCollection? restServers;

            try
            {
                mqttClients = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<MqttClientAgentCollection, MqttClientAgent>(cancellationToken: cancellationToken).ConfigureAwait(false);
                restClients = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<RestClientAgentCollection, RestClientAgent>(cancellationToken: cancellationToken).ConfigureAwait(false);
                restServers = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<RestServerAgentCollection, RestServerAgent>(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                // IoT Gateway plug-in may not be installed on the server
                m_logger.LogDebug(ex, "IoT Gateway plug-in not available, skipping IoT Gateway loading");
                return;
            }

            if ((mqttClients != null && mqttClients.Count > 0) ||
                (restClients != null && restClients.Count > 0) ||
                (restServers != null && restServers.Count > 0))
            {
                project.IotGateway = new IotGatewayContainer
                {
                    MqttClientAgents = mqttClients,
                    RestClientAgents = restClients,
                    RestServerAgents = restServers
                };

                // Load IoT Items for each agent
                var agentTasks = new List<Task>();
                foreach (var agent in (mqttClients ?? []).Cast<IotAgent>()
                    .Concat(restClients ?? [])
                    .Concat(restServers ?? []))
                {
                    agentTasks.Add(Task.Run(async () =>
                    {
                        agent.IotItems = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<IotItemCollection, IotItem>(agent, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }));
                }
                await Task.WhenAll(agentTasks).ConfigureAwait(false);
            }
        }
        #endregion

        #region recursive methods

        private async Task CompareAndApplyIotGatewayDetailedAsync(
            IotGatewayContainer? source, IotGatewayContainer? current,
            ProjectCompareAndApplyResult result, CancellationToken cancellationToken)
        {
            // Compare MQTT Client agents
            var mqttCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<MqttClientAgentCollection, MqttClientAgent>(
                source?.MqttClientAgents, current?.MqttClientAgents, cancellationToken: cancellationToken).ConfigureAwait(false);
            result.Add(mqttCompare);

            foreach (var agent in mqttCompare.CompareResult.UnchangedItems.Concat(mqttCompare.CompareResult.ChangedItems))
            {
                var itemCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<IotItemCollection, IotItem>(
                    agent.Left!.IotItems, agent.Right!.IotItems, agent.Right, cancellationToken).ConfigureAwait(false);
                result.Add(itemCompare);
            }

            // Compare REST Client agents
            var restClientCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<RestClientAgentCollection, RestClientAgent>(
                source?.RestClientAgents, current?.RestClientAgents, cancellationToken: cancellationToken).ConfigureAwait(false);
            result.Add(restClientCompare);

            foreach (var agent in restClientCompare.CompareResult.UnchangedItems.Concat(restClientCompare.CompareResult.ChangedItems))
            {
                var itemCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<IotItemCollection, IotItem>(
                    agent.Left!.IotItems, agent.Right!.IotItems, agent.Right, cancellationToken).ConfigureAwait(false);
                result.Add(itemCompare);
            }

            // Compare REST Server agents
            var restServerCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<RestServerAgentCollection, RestServerAgent>(
                source?.RestServerAgents, current?.RestServerAgents, cancellationToken: cancellationToken).ConfigureAwait(false);
            result.Add(restServerCompare);

            foreach (var agent in restServerCompare.CompareResult.UnchangedItems.Concat(restServerCompare.CompareResult.ChangedItems))
            {
                var itemCompare = await m_kepwareApiClient.GenericConfig.CompareAndApplyDetailedAsync<IotItemCollection, IotItem>(
                    agent.Left!.IotItems, agent.Right!.IotItems, agent.Right, cancellationToken).ConfigureAwait(false);
                result.Add(itemCompare);
            }
        }

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
            var result = await RecusivlyCompareTagGroupDetailed(apiClient, left, right, owner, cancellationToken).ConfigureAwait(false);
            return (result.Inserts, result.Updates, result.Deletes);
        }

        private Task<ProjectCompareAndApplyResult> RecusivlyCompareTagGroupDetailed(DeviceTagGroupCollection left, DeviceTagGroupCollection? right, NamedEntity owner, CancellationToken cancellationToken)
         => RecusivlyCompareTagGroupDetailed(m_kepwareApiClient, left, right, owner, cancellationToken);

        internal static async Task<ProjectCompareAndApplyResult> RecusivlyCompareTagGroupDetailed(KepwareApiClient apiClient, DeviceTagGroupCollection left, DeviceTagGroupCollection? right, NamedEntity owner, CancellationToken cancellationToken)
        {
            var ret = new ProjectCompareAndApplyResult();

            var tagGroupCompare = await apiClient.GenericConfig.CompareAndApplyDetailedAsync<DeviceTagGroupCollection, DeviceTagGroup>(left, right, owner, cancellationToken: cancellationToken).ConfigureAwait(false);
            ret.Add(tagGroupCompare);

            foreach (var tagGroup in tagGroupCompare.CompareResult.UnchangedItems.Concat(tagGroupCompare.CompareResult.ChangedItems))
            {
                var tagGroupTagCompare = await apiClient.GenericConfig.CompareAndApplyDetailedAsync<DeviceTagCollection, Tag>(tagGroup.Left!.Tags, tagGroup.Right!.Tags, tagGroup.Right, cancellationToken: cancellationToken).ConfigureAwait(false);
                ret.Add(tagGroupTagCompare);

                if (tagGroup.Left!.TagGroups != null)
                {
                    var result = await RecusivlyCompareTagGroupDetailed(apiClient, tagGroup.Left!.TagGroups, tagGroup.Right!.TagGroups, tagGroup.Right, cancellationToken: cancellationToken).ConfigureAwait(false);
                    ret.Add(result);
                }
            }

            return ret;
        }

        /// <summary>
        /// Recursively loads tag groups and their tags.
        /// </summary>
        /// <param name="apiClient">The API client.</param>
        /// <param name="tagGroups">The tag groups to load.</param>
        /// <param name="optimizedRecursion">A flag indicating whether to use optimized recursion with content=serialize or basic recursion. 
        /// This is only applicable for projects loaded with the full project load option and when the JsonProjectLoad service is 
        /// supported by the server.</param>
        /// <param name="tagLimit">Tag Limit if overridden by method call.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        internal static async Task LoadTagGroupsRecursiveAsync(KepwareApiClient apiClient, IEnumerable<DeviceTagGroup> tagGroups, bool optimizedRecursion = false, int tagLimit = 0, CancellationToken cancellationToken = default)
        {
            // Falls back to the value set by <see cref="KepwareApiClientOptions.ProjectLoadTagLimit"/> if tagLimit parameter is not set or an invalid value is provided.
            if (tagLimit <= 0)
                tagLimit = apiClient.ClientOptions.ProjectLoadTagLimit;
            foreach (var tagGroup in tagGroups)
            {
                // Load the Tag Groups and Tags of the current Tag Group
                if (optimizedRecursion && tagGroup.TotalTagCount < tagLimit)
                {
                    var query = new[]
                    {
                        new KeyValuePair<string, string?>("content", "serialize")
                    };
                    var loadedTagGroup = await apiClient.GenericConfig.LoadEntityAsync<DeviceTagGroup>(tagGroup.Name, tagGroup.Owner!, query, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (loadedTagGroup != null)
                    {
                        tagGroup.Tags = loadedTagGroup.Tags;
                        tagGroup.TagGroups = loadedTagGroup.TagGroups;
                    }
                    else
                    {
                        // Failed to load tag group, log warning and end without loading child tag groups.
                        apiClient.Logger.LogWarning("Failed to load {TagGroupName} in {OwnerName}", tagGroup.Name, tagGroup.Owner?.Name);
                        continue;
                    }
                }
                else
                {
                    tagGroup.TagGroups = await apiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(tagGroup, cancellationToken: cancellationToken).ConfigureAwait(false);
                    tagGroup.Tags = await apiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(tagGroup, cancellationToken: cancellationToken).ConfigureAwait(false);
                    // Recursively load the Tag Groups and Tags of the child Tag Groups
                    if (tagGroup.TagGroups != null && tagGroup.TagGroups.Count > 0)
                    {
                        await LoadTagGroupsRecursiveAsync(apiClient, tagGroup.TagGroups, optimizedRecursion, tagLimit, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
        #endregion
    }
}
