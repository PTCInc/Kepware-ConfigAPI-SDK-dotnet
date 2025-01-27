﻿using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

namespace Kepware.Api
{
    /// <summary>
    /// Client for interacting with the Kepware server.
    /// </summary>
    public partial class KepwareApiClient : IKepwareDefaultValueProvider
    {
        /// <summary>
        /// The value for an unknown client or host name.
        /// </summary>
        public const string UNKNOWN = "Unknown";
        private const string ENDPOINT_STATUS = "/config/v1/status";
        private const string ENDPOINT_ABOUT = "/config/v1/about";
        private const string ENDPONT_FULL_PROJECT = "/config/v1/project?content=serialize";
        private static readonly Regex s_pathplaceHolderRegex = EndpointPlaceholderRegex();

        private readonly ILogger<KepwareApiClient> m_logger;
        private readonly HttpClient m_httpClient;

        private ReadOnlyDictionary<string, Docs.Driver>? m_cachedSupportedDrivers = null;
        private readonly ConcurrentDictionary<string, Docs.Channel> m_cachedSupportedChannels = [];
        private readonly ConcurrentDictionary<string, Docs.Device> m_cachedSupportedDevices = [];

        private bool? m_blnIsConnected = null;

        /// <summary>
        /// Gets the name of the client.
        /// </summary>
        public string ClientName { get; }

        /// <summary>
        /// Gets the host name of the client.
        /// </summary>
        public string ClientHostName => m_httpClient.BaseAddress?.Host ?? UNKNOWN;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="KepwareApiClient"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="httpClient">The HTTP client instance.</param>
        public KepwareApiClient(ILogger<KepwareApiClient> logger, HttpClient httpClient)
            : this(UNKNOWN, logger, httpClient)
        {
        }

        internal KepwareApiClient(string name, ILogger<KepwareApiClient> logger, HttpClient httpClient)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            ClientName = name;
        }
        #endregion

        #region connection test & product info
        /// <summary>
        /// Tests the connection to the Kepware server.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the connection was successful.</returns>

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            bool blnIsConnected = false;
            try
            {
                if (m_blnIsConnected == null) // first time after connection change
                {
                    m_logger.LogInformation("Connecting to {ClientName}-client at {BaseAddress}...", ClientName, m_httpClient.BaseAddress);
                }
                var response = await m_httpClient.GetAsync(ENDPOINT_STATUS, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var status = await JsonSerializer.DeserializeAsync(
                        await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                        KepJsonContext.Default.ListApiStatus, cancellationToken)
                        .ConfigureAwait(false);
                    if (status?.FirstOrDefault()?.Healthy == true)
                    {
                        blnIsConnected = true;
                    }
                }

                if (m_blnIsConnected == null || (m_blnIsConnected != null && m_blnIsConnected != blnIsConnected)) // first time after connection change or when connection is lost
                {
                    if (!blnIsConnected)
                    {
                        m_logger.LogWarning("Failed to connect to {ClientName}-client at {BaseAddress}, Reason: {ReasonPhrase}", ClientName, m_httpClient.BaseAddress, response.ReasonPhrase);
                    }
                    else
                    {
                        var prodInfo = await GetProductInfoAsync(cancellationToken).ConfigureAwait(false);
                        m_logger.LogInformation("Successfully connected to {ClientName}-client: {ProductName} {ProductVersion} on {BaseAddress}", ClientName, prodInfo?.ProductName, prodInfo?.ProductVersion, m_httpClient.BaseAddress);
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                if (m_blnIsConnected == null || m_blnIsConnected == true) // first time after connection change or when connection is lost
                    m_logger.LogWarning(httpEx, "Failed to connect to {ClientName}-client at {BaseAddress}", ClientName, m_httpClient.BaseAddress);
            }
            m_blnIsConnected = blnIsConnected;
            return blnIsConnected;
        }

        /// <summary>
        /// Gets the product information from the Kepware server.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the product information.</returns>
        public async Task<ProductInfo?> GetProductInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await m_httpClient.GetAsync(ENDPOINT_ABOUT, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var prodInfo = JsonSerializer.Deserialize(content, KepJsonContext.Default.ProductInfo);

                    m_blnIsConnected = true;
                    return prodInfo;
                }
                else
                {
                    m_logger.LogWarning("Failed to get product info from endpoint {Endpoint}, Reason: {ReasonPhrase}", "/config/v1/about", response.ReasonPhrase);
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_blnIsConnected = null;
            }

            return null;
        }
        #endregion

        #region CompareAndApply

        /// <summary>
        /// Compares the source project with the project from the API and applies the changes to the API.
        /// </summary>
        /// <param name="sourceProject"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<(int inserts, int updates, int deletes)> CompareAndApply(Project sourceProject, CancellationToken cancellationToken = default)
        {
            var projectFromApi = await LoadProject(blnLoadFullProject: true, cancellationToken: cancellationToken);
            await projectFromApi.Cleanup(this, true, cancellationToken).ConfigureAwait(false);
            return await CompareAndApply(sourceProject, projectFromApi, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Compares the source project with the project from the API and applies the changes to the API.
        /// </summary>
        /// <param name="sourceProject"></param>
        /// <param name="projectFromApi"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<(int inserts, int updates, int deletes)> CompareAndApply(Project sourceProject, Project projectFromApi, CancellationToken cancellationToken = default)
        {
            if (sourceProject.Hash != projectFromApi.Hash)
            {
                //TODO update project
                m_logger.LogInformation("[not implemented] Project has changed. Updating project...");
            }
            int inserts = 0, updates = 0, deletes = 0;

            var channelCompare = await CompareAndApply<ChannelCollection, Channel>(sourceProject.Channels, projectFromApi.Channels,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            updates += channelCompare.ChangedItems.Count;
            inserts += channelCompare.ItemsOnlyInLeft.Count;
            deletes += channelCompare.ItemsOnlyInRight.Count;

            foreach (var channel in channelCompare.UnchangedItems.Concat(channelCompare.ChangedItems))
            {
                var deviceCompare = await CompareAndApply<DeviceCollection, Device>(channel.Left!.Devices, channel.Right!.Devices, channel.Right,
                cancellationToken: cancellationToken).ConfigureAwait(false);

                updates += deviceCompare.ChangedItems.Count;
                inserts += deviceCompare.ItemsOnlyInLeft.Count;
                deletes += deviceCompare.ItemsOnlyInRight.Count;

                foreach (var device in deviceCompare.UnchangedItems.Concat(deviceCompare.ChangedItems))
                {
                    var tagCompare = await CompareAndApply<DeviceTagCollection, Tag>(device.Left!.Tags, device.Right!.Tags, device.Right, cancellationToken).ConfigureAwait(false);

                    updates += tagCompare.ChangedItems.Count;
                    inserts += tagCompare.ItemsOnlyInLeft.Count;
                    deletes += tagCompare.ItemsOnlyInRight.Count;

                    var tagGroupCompare = await CompareAndApply<DeviceTagGroupCollection, DeviceTagGroup>(device.Left!.TagGroups, device.Right!.TagGroups, device.Right, cancellationToken).ConfigureAwait(false);

                    updates += tagGroupCompare.ChangedItems.Count;
                    inserts += tagGroupCompare.ItemsOnlyInLeft.Count;
                    deletes += tagGroupCompare.ItemsOnlyInRight.Count;


                    foreach (var tagGroup in tagGroupCompare.UnchangedItems.Concat(tagGroupCompare.ChangedItems))
                    {
                        var tagGroupTagCompare = await CompareAndApply<DeviceTagGroupTagCollection, Tag>(tagGroup.Left!.Tags, tagGroup.Right!.Tags, tagGroup.Right, cancellationToken).ConfigureAwait(false);

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


        /// <summary>
        /// Compares two collections of entities and applies the changes to the target collection.
        /// Left should represent the source and Right should represent the API (target).
        /// </summary>
        /// <typeparam name="T">The type of the entity collection.</typeparam>
        /// <typeparam name="K">The type of the entity.</typeparam>
        /// <param name="sourceCollection">The source collection.</param>
        /// <param name="apiCollection">The collection representing the current state of the API</param>
        /// <param name="owner">The owner of the entities.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the comparison result.</returns>

        public async Task<EntityCompare.CollectionResultBucket<K>> CompareAndApply<T, K>(T? sourceCollection, T? apiCollection, NamedEntity? owner = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
        {
            var compareResult = EntityCompare.Compare<T, K>(sourceCollection, apiCollection);

            // This are the items that are in the API but not in the source
            // --> we need to delete them
            await DeleteItemsAsync<T, K>(compareResult.ItemsOnlyInRight.Select(i => i.Right!).ToList(), owner, cancellationToken: cancellationToken).ConfigureAwait(false);

            // This are the items both in the API and the source
            // --> we need to update them
            await UpdateItemsAsync<T, K>(compareResult.ChangedItems.Select(i => (i.Left!, i.Right)).ToList(), owner, cancellationToken: cancellationToken).ConfigureAwait(false);

            // This are the items that are in the source but not in the API
            // --> we need to insert them
            await InsertItemsAsync<T, K>(compareResult.ItemsOnlyInLeft.Select(i => i.Left!).ToList(), owner: owner, cancellationToken: cancellationToken).ConfigureAwait(false);

            return compareResult;
        }
        #endregion

        #region Update
        /// <summary>
        /// Updates an item in the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        /// <param name="item">The item to update.</param>
        /// <param name="oldItem">The old item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the update was successful.</returns>
        public async Task<bool> UpdateItemAsync<T>(T item, T? oldItem = default, CancellationToken cancellationToken = default)
           where T : NamedEntity, new()
        {
            try
            {
                var endpoint = ResolveEndpoint<T>(oldItem ?? item);

                m_logger.LogInformation("Updating {TypeName} on {Endpoint}...", typeof(T).Name, endpoint);

                var currentEntity = await LoadEntityAsync<T>((oldItem ?? item).Flatten().Select(i => i.Name).Reverse(), cancellationToken: cancellationToken).ConfigureAwait(false);
                item.ProjectId = currentEntity?.ProjectId;

                HttpContent httpContent = new StringContent(JsonSerializer.Serialize(item, KepJsonContext.GetJsonTypeInfo<T>()), Encoding.UTF8, "application/json");
                var response = await m_httpClient.PutAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    m_logger.LogError("Failed to update {TypeName} from {Endpoint}: {ReasonPhrase}\n{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
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

        /// <summary>
        /// Updates an item in the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity collection.</typeparam>
        /// <typeparam name="K">The type of the item.</typeparam>
        /// <param name="item">The item to update.</param>
        /// <param name="oldItem">The old item.</param>
        /// <param name="owner">The owner of the entities.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task UpdateItemAsync<T, K>(K item, K? oldItem = default, NamedEntity? owner = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>
            where K : NamedEntity, new()
            => UpdateItemsAsync<T, K>([(item, oldItem)], owner, cancellationToken);

        /// <summary>
        /// Updates a list of items in the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="items"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UpdateItemsAsync<T, K>(List<(K item, K? oldItem)> items, NamedEntity? owner = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return;

            try
            {
                var collectionEndpoint = ResolveEndpoint<T>(owner).TrimEnd('/');
                foreach (var pair in items)
                {
                    var endpoint = $"{collectionEndpoint}/{Uri.EscapeDataString(pair.oldItem!.Name)}";
                    var currentEntity = await LoadEntityByEndpointAsync<K>(endpoint, cancellationToken).ConfigureAwait(false);
                    if (currentEntity == null)
                    {
                        m_logger.LogError("Failed to load {TypeName} from {Endpoint}", typeof(K).Name, endpoint);
                    }
                    else
                    {
                        currentEntity.Owner = owner;
                        pair.item.ProjectId = currentEntity.ProjectId;
                        var diff = pair.item.GetUpdateDiff(currentEntity);

                        m_logger.LogInformation("Updating {TypeName} on {Endpoint}, values {Diff}", typeof(T).Name, endpoint, diff);

                        HttpContent httpContent = new StringContent(JsonSerializer.Serialize(diff, KepJsonContext.Default.DictionaryStringJsonElement), Encoding.UTF8, "application/json");
                        var response = await m_httpClient.PutAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                        {
                            var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                            m_logger.LogError("Failed to update {TypeName} from {Endpoint}: {ReasonPhrase}\n{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
                        }
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_blnIsConnected = null;
            }
        }
        #endregion

        #region Insert
        /// <summary>
        /// Inserts an item in the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="item"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> InsertItemAsync<T, K>(K item, NamedEntity? owner = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
            => (await InsertItemsAsync<T, K>([item], owner: owner, cancellationToken: cancellationToken)).FirstOrDefault();

        /// <summary>
        /// Inserts a list of items in the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="items"></param>
        /// <param name="pageSize"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool[]> InsertItemsAsync<T, K>(List<K> items, int pageSize = 10, NamedEntity? owner = null, CancellationToken cancellationToken = default)
         where T : EntityCollection<K>
         where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return [];

            List<bool> result = new List<bool>();

            try
            {
                var endpoint = ResolveEndpoint<T>(owner);


                if (typeof(K) == typeof(Channel) || typeof(K) == typeof(Device))
                {
                    //check for usage of non supported drivers
                    var drivers = await SupportedDriversAsync(cancellationToken);

                    var groupedItems = items
                      .GroupBy(i =>
                      {
                          var driver = i.GetDynamicProperty<string>(Properties.DeviceDriver);
                          return !string.IsNullOrEmpty(driver) && drivers.ContainsKey(driver);
                      });

                    var unsupportedItems = groupedItems.FirstOrDefault(g => !g.Key)?.ToList() ?? [];
                    if (unsupportedItems.Count > 0)
                    {
                        items = groupedItems.FirstOrDefault(g => g.Key)?.ToList() ?? [];
                        m_logger.LogWarning("The following {NumItems} {TypeName} have unsupported drivers ({ListOfUsedUnsupportedDrivers}) and will not be inserted: {ItemsNames}",
                            unsupportedItems.Count, typeof(K).Name, unsupportedItems.Select(i => i.GetDynamicProperty<string>(Properties.DeviceDriver)).Distinct(), unsupportedItems.Select(i => i.Name));
                    }
                }

                var totalPageCount = (int)Math.Ceiling((double)items.Count / pageSize);
                for (int i = 0; i < totalPageCount; i++)
                {
                    var pageItems = items.Skip(i * pageSize).Take(pageSize).ToList();
                    m_logger.LogInformation("Inserting {NumItems} {TypeName} on {Endpoint} in batch {BatchNr} of {TotalBatches} ...", pageItems.Count, typeof(K).Name, endpoint, i + 1, totalPageCount);

                    var jsonContent = JsonSerializer.Serialize(pageItems, KepJsonContext.GetJsonListTypeInfo<K>());
                    HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var response = await m_httpClient.PostAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        m_logger.LogError("Failed to insert {TypeName} from {Endpoint}: {ReasonPhrase}\n{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
                        result.AddRange(Enumerable.Repeat(false, pageItems.Count));
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.MultiStatus)
                    {
                        // When a POST includes multiple objects, if one or more cannot be processed due to a parsing failure or 
                        // some other non - property validation error, the HTTPS status code 207(Multi - Status) will be returned along
                        // with a JSON object array containing the status for each object in the request.
                        var results = await JsonSerializer.DeserializeAsync(
                            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                            KepJsonContext.Default.ListApiResult, cancellationToken).ConfigureAwait(false) ?? [];

                        result.AddRange(results.Select(r => r.IsSuccessStatusCode));

                        var failedEntries = results?.Where(r => !r.IsSuccessStatusCode)?.ToList() ?? [];
                        m_logger.LogError("{NumSuccessFull} were successfull, failed to insert {NumFailed} {TypeName} from {Endpoint}: {ReasonPhrase}\nFailed:\n{Message}",
                            (results?.Count ?? 0) - failedEntries.Count, failedEntries.Count, typeof(T).Name, endpoint, response.ReasonPhrase, JsonSerializer.Serialize(failedEntries, KepJsonContext.Default.ListApiResult));
                    }
                    else
                    {
                        result.AddRange(Enumerable.Repeat(true, pageItems.Count));
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_blnIsConnected = null;

                if (items.Count > result.Count)
                    result.AddRange(Enumerable.Repeat(false, items.Count - result.Count));
            }

            return [.. result];
        }
        #endregion

        #region Delete
        /// <summary>
        /// Deletes an item from the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> DeleteItemAsync<T>(T item, CancellationToken cancellationToken = default)
          where T : NamedEntity, new()
        {
            var endpoint = ResolveEndpoint<T>(item).TrimEnd('/');
            m_logger.LogInformation("Deleting {TypeName} on {Endpoint}...", item.Name, endpoint);
            try
            {
                var response = await m_httpClient.DeleteAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    m_logger.LogError("Failed to delete {TypeName} from {Endpoint}: {ReasonPhrase}\n{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
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
        /// <summary>
        /// Deletes an item from the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="item"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task DeleteItemAsync<T, K>(K item, NamedEntity? owner = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>
            where K : NamedEntity, new()
            => DeleteItemsAsync<T, K>([item], owner, cancellationToken);

        /// <summary>
        /// Deletes a list of items from the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="items"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>

        public async Task DeleteItemsAsync<T, K>(List<K> items, NamedEntity? owner = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>
            where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return;
            try
            {
                var collectionEndpoint = ResolveEndpoint<T>(owner).TrimEnd('/');
                foreach (var item in items)
                {
                    var endpoint = $"{collectionEndpoint}/{Uri.EscapeDataString(item.Name)}";

                    m_logger.LogInformation("Deleting {TypeName} on {Endpoint}...", typeof(K).Name, endpoint);

                    var response = await m_httpClient.DeleteAsync(endpoint, cancellationToken).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        m_logger.LogError("Failed to delete {TypeName} from {Endpoint}: {ReasonPhrase}\n{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_blnIsConnected = null;
            }
        }
        #endregion

        #region Load

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

        #region LoadEntity

        public Task<T?> LoadEntityAsync<T>(string? name = default, CancellationToken cancellationToken = default)
        where T : BaseEntity, new()
        {
            var endpoint = ResolveEndpoint<T>(string.IsNullOrEmpty(name) ? [] : [name]);
            return LoadEntityByEndpointAsync<T>(endpoint, cancellationToken);
        }

        public Task<T?> LoadEntityAsync<T>(IEnumerable<string> owner, CancellationToken cancellationToken = default)
         where T : BaseEntity, new()
        {
            var endpoint = ResolveEndpoint<T>(owner);
            return LoadEntityByEndpointAsync<T>(endpoint, cancellationToken);
        }

        public Task<T?> LoadEntityAsync<T>(string name, NamedEntity owner, CancellationToken cancellationToken = default)
         where T : BaseEntity, new()
         => LoadEntityAsync<T>(name, owner, queryParams: null, cancellationToken: cancellationToken);

        private async Task<T?> LoadEntityAsync<T>(string name, NamedEntity owner, IEnumerable<KeyValuePair<string, string>>? queryParams = null, CancellationToken cancellationToken = default)
          where T : BaseEntity, new()
        {
            var endpoint = ResolveEndpoint<T>([.. owner.Flatten().Select(n => n.Name).Reverse(), name]);
            if (queryParams != null)
            {
                var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                endpoint += "?" + queryString;
            }
            var entity = await LoadEntityByEndpointAsync<T>(endpoint, cancellationToken);

            if (entity is IHaveOwner ownable)
            {
                ownable.Owner = owner;
            }
            return entity;
        }

        private async Task<T?> LoadEntityByEndpointAsync<T>(string endpoint, CancellationToken cancellationToken = default)
          where T : BaseEntity, new()
        {
            try
            {
                m_logger.LogDebug("Loading {TypeName} from {Endpoint}...", typeof(T).Name, endpoint);

                var response = await m_httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    m_logger.LogError("Failed to load {TypeName} from {Endpoint}: {ReasonPhrase}", typeof(T).Name, endpoint, response.ReasonPhrase);
                    return default;
                }

                var entity = await DeserializeJsonAsync<T>(response, cancellationToken).ConfigureAwait(false);

                return entity;
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_blnIsConnected = null;
                return default;
            }
        }
        #endregion

        #region LoadCollection

        public Task<T?> LoadCollectionAsync<T>(NamedEntity? owner = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<DefaultEntity>, new()
         => LoadCollectionAsync<T, DefaultEntity>(owner, cancellationToken);

        public async Task<T?> LoadCollectionAsync<T, K>(NamedEntity? owner = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>, new()
            where K : BaseEntity, new()
        {
            var endpoint = ResolveEndpoint<T>(owner);
            try
            {
                m_logger.LogDebug("Loading {TypeName} from {Endpoint}...", typeof(T).Name, endpoint);
                var response = await m_httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    m_logger.LogError("Failed to load {TypeName} from {Endpoint}: {ReasonPhrase}", typeof(T).Name, endpoint, response.ReasonPhrase);
                    return default;
                }

                var collection = await DeserializeJsonArrayAsync<K>(response);
                if (collection != null)
                {
                    // if generic type K implements IHaveOwner
                    if (collection.OfType<IHaveOwner>().Any())
                    {
                        foreach (var item in collection.OfType<IHaveOwner>())
                        {
                            item.Owner = owner;
                        }
                    }

                    var resultCollection = new T() { Owner = owner };
                    resultCollection.AddRange(collection);
                    return resultCollection;
                }
                else
                {
                    m_logger.LogError("Failed to deserialize {TypeName} from {Endpoint}", typeof(T).Name, endpoint);
                    return default;
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_blnIsConnected = null;
                return default;
            }
        }
        #endregion

        #region docs
        public async Task<ReadOnlyDictionary<string, Docs.Driver>> SupportedDriversAsync(CancellationToken cancellationToken = default)
        {
            if (m_cachedSupportedDrivers == null)
            {
                var drivers = await LoadSupportedDriversAsync(cancellationToken).ConfigureAwait(false);

                m_cachedSupportedDrivers = drivers.Where(d => !string.IsNullOrEmpty(d.DisplayName)).ToDictionary(d => d.DisplayName!).AsReadOnly();
            }
            return m_cachedSupportedDrivers;
        }

        public Task<Docs.Channel> GetChannelPropertiesAsync(Docs.Driver driver, CancellationToken cancellationToken = default)
         => GetChannelPropertiesAsync(driver.DisplayName!, cancellationToken);

        public async Task<Docs.Channel> GetChannelPropertiesAsync(string driverName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(driverName))
            {
                throw new ArgumentNullException(nameof(driverName));
            }

            if (m_cachedSupportedChannels.TryGetValue(driverName, out var channels))
            {
                return channels;
            }
            else
            {
                m_cachedSupportedChannels[driverName] = channels = await LoadChannelPropertiesAsync(driverName, cancellationToken).ConfigureAwait(false);
                return channels;
            }
        }

        public Task<Docs.Device> GetDevicePropertiesAsync(Docs.Driver driver, CancellationToken cancellationToken = default)
            => GetDevicePropertiesAsync(driver.DisplayName!, cancellationToken);

        public async Task<Docs.Device> GetDevicePropertiesAsync(string driverName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(driverName))
            {
                throw new ArgumentNullException(nameof(driverName));
            }

            if (m_cachedSupportedDevices.TryGetValue(driverName, out var devices))
            {
                return devices;
            }
            else
            {
                m_cachedSupportedDevices[driverName] = devices = await LoadDevicePropertiesAsync(driverName, cancellationToken).ConfigureAwait(false);
                return devices;
            }
        }
        protected virtual async Task<List<Docs.Driver>> LoadSupportedDriversAsync(CancellationToken cancellationToken = default)
        {
            var endpoint = ResolveEndpoint<Docs.Driver>();

            var response = await m_httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to load drivers from {endpoint}: {response.ReasonPhrase}");
            }

            return await DeserializeJsonAsync(response, KepDocsJsonContext.Default.ListDriver, cancellationToken).ConfigureAwait(false) ?? [];
        }

        protected virtual async Task<Docs.Device> LoadDevicePropertiesAsync(string driverName, CancellationToken cancellationToken)
        {
            var endpoint = ResolveEndpoint<Docs.Device>([driverName]);

            var response = await m_httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to load device properties from {endpoint}: {response.ReasonPhrase}");
            }

            return await DeserializeJsonAsync(response, KepDocsJsonContext.Default.Device, cancellationToken).ConfigureAwait(false) ??
                throw new HttpRequestException($"Failed to load device properties from {endpoint}: unable to desrialze");
        }

        protected virtual async Task<Docs.Channel> LoadChannelPropertiesAsync(string driverName, CancellationToken cancellationToken)
        {
            var endpoint = ResolveEndpoint<Docs.Channel>([driverName]);

            var response = await m_httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to load channel properties from {endpoint}: {response.ReasonPhrase}");
            }

            return await DeserializeJsonAsync(response, KepDocsJsonContext.Default.Channel, cancellationToken).ConfigureAwait(false) ??
                throw new HttpRequestException($"Failed to load channel properties from {endpoint}: unable to desrialze");
        }
        #endregion

        #endregion

        #region ResolveEndpoint
        public static string ResolveEndpoint<T>()
            => ResolveEndpoint<T>([]);
        /// <summary>
        /// Resolves the endpoint for the specified entity type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="owner"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string ResolveEndpoint<T>(NamedEntity? owner)
        {
            var endpointTemplateAttribute = typeof(T).GetCustomAttributes(typeof(EndpointAttribute), false)
                .OfType<EndpointAttribute>()
                .FirstOrDefault();

            if (endpointTemplateAttribute == null)
            {
                throw new InvalidOperationException($"No endpoint defined for {typeof(T).Name}");
            }

            if (endpointTemplateAttribute is RecursiveEndpointAttribute recursiveEndpointAttribute && recursiveEndpointAttribute.RecursiveOwnerType == owner?.GetType())
            {
                return ResolveRecursiveEndpoint(recursiveEndpointAttribute, owner) + endpointTemplateAttribute.Suffix;
            }

            return ReplacePlaceholders(endpointTemplateAttribute.EndpointTemplate, owner?.Flatten().Select(n => n.Name).Reverse() ?? []) + endpointTemplateAttribute.Suffix;
        }

        /// <summary>
        /// Resolves the endpoint for the specified entity type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="placeholderValues"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string ResolveEndpoint<T>(IEnumerable<string> placeholderValues)
        {
            var endpointTemplateAttribute = typeof(T).GetCustomAttributes(typeof(EndpointAttribute), false)
                .OfType<EndpointAttribute>()
                .FirstOrDefault();

            if (endpointTemplateAttribute == null)
            {
                throw new InvalidOperationException($"No endpoint defined for {typeof(T).Name}");
            }

            return ReplacePlaceholders(endpointTemplateAttribute.EndpointTemplate, placeholderValues) + endpointTemplateAttribute.Suffix;
        }

        private static string ReplacePlaceholders(string template, IEnumerable<string> placeholderValues)
        {
            var placeholders = s_pathplaceHolderRegex.Matches(template).ToArray();
            var values = placeholderValues.ToArray();
            if (placeholders.Length != values.Length)
            {
                throw new InvalidOperationException($"The number of placeholders in the template '{template}' does not match the number of values ({string.Join(",", values)}).");
            }

            foreach (var match in placeholders.Zip(values, (placeholder, value) => (placeholder, value)))
            {
                template = template.Replace(match.placeholder.Value, Uri.EscapeDataString(match.value));
            }

            return template;
        }

        private static string ResolveRecursiveEndpoint(RecursiveEndpointAttribute attribute, NamedEntity? owner)
        {
            LinkedList<string> recursivePath = new LinkedList<string>();
            while (owner != null && attribute.RecursiveOwnerType == owner?.GetType())
            {
                var currentEndpointPart = ReplacePlaceholders(attribute.RecursiveEnd, [owner.Name]);
                recursivePath.AddFirst(currentEndpointPart);

                if (owner is IHaveOwner ownable && ownable.Owner is NamedEntity nextOwner)
                    owner = nextOwner;
                else
                    owner = null;
            }

            // Combine with the base endpoint template 
            var baseEndpoint = ReplacePlaceholders(attribute.EndpointTemplate, owner?.Flatten().Select(n => n.Name).Reverse() ?? []);

            return baseEndpoint + string.Concat(recursivePath);
        }

        [GeneratedRegex(@"\{(.+?)\}", RegexOptions.Compiled)]
        private static partial Regex EndpointPlaceholderRegex();
        #endregion

        #region private methods

        #region deserialize
        protected Task<K?> DeserializeJsonAsync<K>(HttpResponseMessage httpResponse, CancellationToken cancellationToken = default)
          where K : BaseEntity, new() => DeserializeJsonAsync<K>(httpResponse, KepJsonContext.GetJsonTypeInfo<K>(), cancellationToken);

        protected async Task<K?> DeserializeJsonAsync<K>(HttpResponseMessage httpResponse, JsonTypeInfo<K> jsonTypeInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync(stream, jsonTypeInfo, cancellationToken);
            }
            catch (JsonException ex)
            {
                m_logger.LogError(ex, "JSON Deserialization failed");
                return default;
            }
        }


        protected async Task<List<K>?> DeserializeJsonArrayAsync<K>(HttpResponseMessage httpResponse, CancellationToken cancellationToken = default)
            where K : BaseEntity, new()
        {
            try
            {
                using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync(stream, KepJsonContext.GetJsonListTypeInfo<K>(), cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                m_logger.LogError(ex, "JSON Deserialization failed");
                return null;
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

        private async Task<(int inserts, int updates, int deletes)> RecusivlyCompareTagGroup(DeviceTagGroupCollection left, DeviceTagGroupCollection? right, NamedEntity owner, CancellationToken cancellationToken)
        {
            (int inserts, int updates, int deletes) ret = (0, 0, 0);

            var tagGroupCompare = await CompareAndApply<DeviceTagGroupCollection, DeviceTagGroup>(left, right, owner, cancellationToken: cancellationToken).ConfigureAwait(false);

            ret.inserts = tagGroupCompare.ItemsOnlyInLeft.Count;
            ret.updates = tagGroupCompare.ChangedItems.Count;
            ret.deletes = tagGroupCompare.ItemsOnlyInRight.Count;

            foreach (var tagGroup in tagGroupCompare.UnchangedItems.Concat(tagGroupCompare.ChangedItems))
            {
                var tagGroupTagCompare = await CompareAndApply<DeviceTagGroupTagCollection, Tag>(tagGroup.Left!.Tags, tagGroup.Right!.Tags, tagGroup.Right, cancellationToken: cancellationToken).ConfigureAwait(false);

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

        internal async Task LoadTagGroupsRecursiveAsync(IEnumerable<DeviceTagGroup> tagGroups, CancellationToken cancellationToken = default)
        {
            foreach (var tagGroup in tagGroups)
            {
                // Lade die TagGroups der aktuellen TagGroup
                tagGroup.TagGroups = await LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(tagGroup, cancellationToken).ConfigureAwait(false);
                tagGroup.Tags = await LoadCollectionAsync<DeviceTagGroupTagCollection, Tag>(tagGroup, cancellationToken).ConfigureAwait(false);

                // Rekursiver Aufruf für die geladenen TagGroups
                if (tagGroup.TagGroups != null && tagGroup.TagGroups.Count > 0)
                {
                    await LoadTagGroupsRecursiveAsync(tagGroup.TagGroups, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        #endregion
        #endregion

        #region IKepwareDefaultValueProvider
        private readonly ConcurrentDictionary<string, ReadOnlyDictionary<string, JsonElement>> m_driverDefaultValues = [];
        async Task<ReadOnlyDictionary<string, JsonElement>> IKepwareDefaultValueProvider.GetDefaultValuesAsync(string driverName, string entityName, CancellationToken cancellationToken)
        {
            var key = $"{driverName}/{entityName}";
            if (m_driverDefaultValues.TryGetValue(key, out var deviceDefaults))
            {
                return deviceDefaults;
            }
            else
            {
                Docs.CollectionDefinition collectionDefinition = entityName switch
                {
                    nameof(Channel) => await GetChannelPropertiesAsync(driverName, cancellationToken),
                    nameof(Device) => await GetDevicePropertiesAsync(driverName, cancellationToken),
                    _ => Docs.CollectionDefinition.Empty,
                };

                var defaults = collectionDefinition?.PropertyDefinitions?
                    .Where(p => !string.IsNullOrEmpty(p.SymbolicName) && p.SymbolicName != Properties.DeviceDriver)
                    .ToDictionary(p => p.SymbolicName!, p => p.GetDefaultValue()) ?? [];

                return m_driverDefaultValues[key] = new ReadOnlyDictionary<string, JsonElement>(defaults);
            }
        }

        #endregion
    }
}
