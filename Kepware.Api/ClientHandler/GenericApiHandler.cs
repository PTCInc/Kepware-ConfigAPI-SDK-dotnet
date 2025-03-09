using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Kepware.Api.ClientHandler
{
    public class GenericApiHandler
    {
        private readonly ILogger<GenericApiHandler> m_logger;
        private readonly HttpClient m_httpClient;
        private readonly KepwareApiClient m_kepwareApiClient;


        private ReadOnlyDictionary<string, Docs.Driver>? m_cachedSupportedDrivers = null;
        private readonly ConcurrentDictionary<string, Docs.Channel> m_cachedSupportedChannels = [];
        private readonly ConcurrentDictionary<string, Docs.Device> m_cachedSupportedDevices = [];


        internal GenericApiHandler(KepwareApiClient kepwareApiClient, ILogger<GenericApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_httpClient = kepwareApiClient.HttpClient;
            m_logger = logger;
        }

        #region CompareAndApply
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
            var endpoint = EndpointResolver.ResolveEndpoint<T>(oldItem ?? item);

            m_logger.LogInformation("Updating {TypeName} on {Endpoint}...", typeof(T).Name, endpoint);

            var currentEntity = await LoadEntityAsync<T>((oldItem ?? item).Flatten().Select(i => i.Name).Reverse(), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (currentEntity == null)
            {
                return false; // Entity not found, update not possible
            }
            return await UpdateItemAsync(endpoint, item, currentEntity, cancellationToken).ConfigureAwait(false);
        }
        protected internal async Task<bool> UpdateItemAsync<T>(string endpoint, T item, T currentEntity, CancellationToken cancellationToken = default)
           where T : NamedEntity, new()
        {
            try
            {
                item.ProjectId = currentEntity.ProjectId;

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
                m_kepwareApiClient.OnHttpRequestException(httpEx);
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
        public async Task<bool> UpdateItemAsync<T, K>(K item, K? oldItem = default, NamedEntity? owner = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>
            where K : NamedEntity, new()
            => (await UpdateItemsAsync<T, K>([(item, oldItem)], owner, cancellationToken)).FirstOrDefault();

        /// <summary>
        /// Updates a list of items in the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="items"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<bool>> UpdateItemsAsync<T, K>(List<(K item, K? oldItem)> items, NamedEntity? owner = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return [];

            List<bool> result = new List<bool>();
            try
            {
                var collectionEndpoint = EndpointResolver.ResolveEndpoint<T>(owner).TrimEnd('/');
                foreach (var pair in items)
                {
                    var endpoint = $"{collectionEndpoint}/{Uri.EscapeDataString(pair.oldItem!.Name)}";
                    var currentEntity = await LoadEntityByEndpointAsync<K>(endpoint, cancellationToken).ConfigureAwait(false);
                    if (currentEntity == null)
                    {
                        m_logger.LogError("Failed to load {TypeName} from {Endpoint}", typeof(K).Name, endpoint);
                        result.Add(false);
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
                            result.Add(false);
                        }
                        else
                        {
                            result.Add(true);
                        }
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }

            if (result.Count < items.Count)
                result.AddRange(Enumerable.Repeat(false, items.Count - result.Count));

            return result;
        }
        #endregion

        #region Insert

        public async Task<bool> InsertItemAsync<T>(T item, NamedEntity? owner = null, CancellationToken cancellationToken = default)
         where T : NamedEntity
        {
            try
            {
                var endpoint = EndpointResolver.ResolveEndpoint<T>(owner, item.Name).TrimEnd('/');

                if (endpoint.EndsWith("/" + item.Name))
                    endpoint = endpoint[..(endpoint.Length - item.Name.Length - 1)];

                var jsonContent = JsonSerializer.Serialize(item, KepJsonContext.GetJsonTypeInfo<T>());
                HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await m_httpClient.PostAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    m_logger.LogError("Failed to insert {TypeName} to {Endpoint}: {ReasonPhrase}\n{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
                }
                else
                {
                    return true;
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);

            }
            return false;
        }

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
                var endpoint = EndpointResolver.ResolveEndpoint<T>(owner);


                if (typeof(K) == typeof(Channel) || typeof(K) == typeof(Device))
                {
                    //check for usage of non supported drivers
                    var drivers = await SupportedDriversAsync(cancellationToken);

                    var groupedItems = items
                      .GroupBy(i =>
                      {
                          var driver = i.GetDynamicProperty<string>(Properties.Channel.DeviceDriver);
                          return !string.IsNullOrEmpty(driver) && drivers.ContainsKey(driver);
                      });

                    var unsupportedItems = groupedItems.FirstOrDefault(g => !g.Key)?.ToList() ?? [];
                    if (unsupportedItems.Count > 0)
                    {
                        items = groupedItems.FirstOrDefault(g => g.Key)?.ToList() ?? [];
                        m_logger.LogWarning("The following {NumItems} {TypeName} have unsupported drivers ({ListOfUsedUnsupportedDrivers}) and will not be inserted: {ItemsNames}",
                            unsupportedItems.Count, typeof(K).Name, unsupportedItems.Select(i => i.GetDynamicProperty<string>(Properties.Channel.DeviceDriver)).Distinct(), unsupportedItems.Select(i => i.Name));
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
                m_kepwareApiClient.OnHttpRequestException(httpEx);

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
        public Task<bool> DeleteItemAsync<T>(T item, CancellationToken cancellationToken = default)
          where T : NamedEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(item).TrimEnd('/');
            return DeleteItemByEndpointAsync<T>(endpoint, cancellationToken);
        }

        public Task<bool> DeleteItemAsync<T>(string itemName, CancellationToken cancellationToken = default)
         where T : NamedEntity, new()
         => DeleteItemAsync<T>([itemName], cancellationToken);
        public Task<bool> DeleteItemAsync<T>(string[] itemNames, CancellationToken cancellationToken = default)
         where T : NamedEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(itemNames).TrimEnd('/');
            return DeleteItemByEndpointAsync<T>(endpoint, cancellationToken);
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

        protected async Task<bool> DeleteItemByEndpointAsync<T>(string endpoint, CancellationToken cancellationToken = default)
         where T : NamedEntity, new()
        {
            try
            {
                m_logger.LogInformation("Deleting {TypeName} on {Endpoint}...", typeof(T).Name, endpoint);
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
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }
            return false;
        }

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
                var collectionEndpoint = EndpointResolver.ResolveEndpoint<T>(owner).TrimEnd('/');
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
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }
        }
        #endregion

        #region Load
        #region LoadEntity

        public Task<T?> LoadEntityAsync<T>(string? name = default, CancellationToken cancellationToken = default)
            where T : BaseEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(string.IsNullOrEmpty(name) ? [] : [name]);
            return LoadEntityByEndpointAsync<T>(endpoint, cancellationToken);
        }

        public Task<T?> LoadEntityAsync<T>(IEnumerable<string> owner, CancellationToken cancellationToken = default)
            where T : BaseEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(owner);
            return LoadEntityByEndpointAsync<T>(endpoint, cancellationToken);
        }

        public async Task<T?> LoadEntityAsync<T>(string name, NamedEntity owner, CancellationToken cancellationToken = default)
            where T : BaseEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(owner, name);

            var entity = await LoadEntityByEndpointAsync<T>(endpoint, cancellationToken);

            if (entity is IHaveOwner ownable)
            {
                ownable.Owner = owner;
            }
            return entity;
        }

        protected internal async Task<T?> LoadEntityByEndpointAsync<T>(string endpoint, CancellationToken cancellationToken = default)
            where T : BaseEntity, new()
        {
            try
            {
                m_logger.LogDebug("Loading {TypeName} from {Endpoint}...", typeof(T).Name, endpoint);

                var response = await m_httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    m_logger.LogWarning("Failed to load {TypeName} from {Endpoint}: {ReasonPhrase}", typeof(T).Name, endpoint, response.ReasonPhrase);
                    return default;
                }

                var entity = await DeserializeJsonAsync<T>(response, cancellationToken).ConfigureAwait(false);

                return entity;
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
                return default;
            }
        }
        #endregion

        #region LoadCollection
        public Task<T?> LoadCollectionAsync<T>(string? owner = default, CancellationToken cancellationToken = default)
             where T : EntityCollection<DefaultEntity>, new()
         => LoadCollectionAsync<T, DefaultEntity>(owner, cancellationToken);
        public Task<T?> LoadCollectionAsync<T>(NamedEntity owner, CancellationToken cancellationToken = default)
          where T : EntityCollection<DefaultEntity>, new()
         => LoadCollectionAsync<T, DefaultEntity>(owner, cancellationToken);
        public Task<T?> LoadCollectionAsync<T, K>(string? owner = default, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>, new()
            where K : BaseEntity, new()
            => LoadCollectionAsync<T, K>(string.IsNullOrEmpty(owner) ? [] : [owner], cancellationToken);
        public async Task<T?> LoadCollectionAsync<T, K>(NamedEntity owner, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>, new()
            where K : BaseEntity, new()
        {
            var collection = await LoadCollectionByEndpointAsync<T, K>(EndpointResolver.ResolveEndpoint<T>(owner), cancellationToken);
            if (collection != null)
            {
                collection.Owner = owner;
                foreach (var item in collection.OfType<IHaveOwner>())
                {
                    item.Owner = owner;
                }
            }
            return collection;
        }

        public Task<T?> LoadCollectionAsync<T, K>(IEnumerable<string> owner, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>, new()
            where K : BaseEntity, new()
            => LoadCollectionByEndpointAsync<T, K>(EndpointResolver.ResolveEndpoint<T>(owner), cancellationToken);

        protected internal async Task<T?> LoadCollectionByEndpointAsync<T, K>(string endpoint, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>, new()
            where K : BaseEntity, new()
        {
            try
            {
                m_logger.LogDebug("Loading {TypeName} from {Endpoint}...", typeof(T).Name, endpoint);
                var response = await m_httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    m_logger.LogWarning("Failed to load {TypeName} from {Endpoint}: {ReasonPhrase}", typeof(T).Name, endpoint, response.ReasonPhrase);
                    return default;
                }

                var collection = await DeserializeJsonArrayAsync<K>(response);
                if (collection != null)
                {
                    var result = new T();
                    result.AddRange(collection);
                    return result;
                }
                else
                {
                    m_logger.LogWarning("Failed to deserialize {TypeName} from {Endpoint}", typeof(T).Name, endpoint);
                    return default;
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
                return default;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to load {TypeName} from {Endpoint}", typeof(T).Name, endpoint);
                throw new InvalidOperationException($"Failed to load {typeof(T).Name} from {endpoint}", ex);
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
            var endpoint = EndpointResolver.ResolveEndpoint<Docs.Driver>();

            var response = await m_httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to load drivers from {endpoint}: {response.ReasonPhrase}");
            }

            return await DeserializeJsonAsync(response, KepDocsJsonContext.Default.ListDriver, cancellationToken).ConfigureAwait(false) ?? [];
        }

        protected virtual async Task<Docs.Device> LoadDevicePropertiesAsync(string driverName, CancellationToken cancellationToken)
        {
            var endpoint = EndpointResolver.ResolveEndpoint<Docs.Device>([driverName]);

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
            var endpoint = EndpointResolver.ResolveEndpoint<Docs.Channel>([driverName]);

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

        #endregion

    }
}
