using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Kepware.Api.ClientHandler
{
    /// <summary>
    /// Provides general operation APIs related to configuring objects in the Kepware server.
    /// </summary>
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
        /// Left should represent the source collection and Right should represent the target collection in the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity collection.</typeparam>
        /// <typeparam name="K">The type of the entity.</typeparam>
        /// <param name="sourceCollection">The source collection.</param>
        /// <param name="targetCollection">The collection representing the current state of the API</param>
        /// <param name="owner">The owner of the entities.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the comparison result as <see cref="EntityCompare.CollectionResultBucket{K}"/>.</returns>

        public async Task<EntityCompare.CollectionResultBucket<K>> CompareAndApply<T, K>(T? sourceCollection, T? targetCollection, NamedEntity? owner = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
        {
            var result = await CompareAndApplyDetailed<T, K>(sourceCollection, targetCollection, owner, cancellationToken).ConfigureAwait(false);
            return result.CompareResult;
        }

        /// <summary>
        /// Compares two collections and applies changes while returning detailed success and failure information.
        /// Left should represent the source collection and Right should represent the target collection in the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity collection.</typeparam>
        /// <typeparam name="K">The type of the entity.</typeparam>
        /// <param name="sourceCollection">The source collection.</param>
        /// <param name="targetCollection">The collection representing the current state of the API.</param>
        /// <param name="owner">The owner of the entities.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A detailed apply result including successful counts and failed item details.</returns>
        public async Task<CollectionApplyResult<K>> CompareAndApplyDetailed<T, K>(T? sourceCollection, T? targetCollection, NamedEntity? owner = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
        {
            var compareResult = EntityCompare.Compare<T, K>(sourceCollection, targetCollection);
            var result = new CollectionApplyResult<K>(compareResult);

            var deleteItems = compareResult.ItemsOnlyInRight.Select(i => i.Right!).ToList();
            var deleteResult = await DeleteItemsAsync<T, K>(deleteItems, owner, cancellationToken: cancellationToken).ConfigureAwait(false);
            for (int i = 0; i < deleteItems.Count; i++)
            {
                if (i < deleteResult.Length && deleteResult[i])
                {
                    result.AddDeleteSuccess();
                }
                else
                {
                    result.AddFailure(new ApplyFailure
                    {
                        Operation = ApplyOperation.Delete,
                        AttemptedItem = deleteItems[i],
                    });
                }
            }

            var updatePairs = compareResult.ChangedItems.Select(i => (i.Left!, i.Right)).ToList();
            var updateResult = await UpdateItemsDetailedAsync<T, K>(updatePairs, owner, cancellationToken).ConfigureAwait(false);
            for (int i = 0; i < updatePairs.Count; i++)
            {
                if (i < updateResult.Count && updateResult[i].IsSuccess)
                {
                    result.AddUpdateSuccess();
                }
                else
                {
                    var updateOutcome = i < updateResult.Count ? updateResult[i] : new UpdateItemOutcome(false);
                    result.AddFailure(new ApplyFailure
                    {
                        Operation = ApplyOperation.Update,
                        AttemptedItem = updatePairs[i].Item1,
                        ResponseCode = updateOutcome.ResponseCode,
                        ResponseMessage = updateOutcome.ResponseMessage,
                        NotAppliedProperties = updateOutcome.NotAppliedProperties,
                    });
                }
            }

            var insertItems = compareResult.ItemsOnlyInLeft.Select(i => i.Left!).ToList();
            var insertResult = await InsertItemsDetailedAsync<T, K>(insertItems, owner: owner, cancellationToken: cancellationToken).ConfigureAwait(false);
            for (int i = 0; i < insertItems.Count; i++)
            {
                if (i < insertResult.Count && insertResult[i].IsSuccess)
                {
                    result.AddInsertSuccess();
                }
                else
                {
                    var insertOutcome = i < insertResult.Count ? insertResult[i] : new InsertItemOutcome(false);
                    result.AddFailure(new ApplyFailure
                    {
                        Operation = ApplyOperation.Insert,
                        AttemptedItem = insertItems[i],
                        ResponseCode = insertOutcome.ResponseCode,
                        ResponseMessage = insertOutcome.ResponseMessage,
                        Property = insertOutcome.Property,
                        Description = insertOutcome.Description,
                        ErrorLine = insertOutcome.ErrorLine,
                    });
                }
            }

            return result;
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
                    var updateMessage = await TryDeserializeUpdateMessageAsync(response, cancellationToken).ConfigureAwait(false);
                    if (updateMessage?.NotApplied != null && updateMessage.NotApplied.Count > 0)
                    {
                        m_logger.LogError("Partial update detected for {TypeName} on {Endpoint}. Not applied properties: {NotApplied}",
                            typeof(T).Name, endpoint, updateMessage.NotApplied.Keys);
                        return false;
                    }

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
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the update was successful.</returns>
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
        /// <returns>The task result contains an array of booleans indicating whether the update for each item was successful.</returns>
        public async Task<bool[]> UpdateItemsAsync<T, K>(List<(K item, K? oldItem)> items, NamedEntity? owner = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
            => (await UpdateItemsDetailedAsync<T, K>(items, owner, cancellationToken).ConfigureAwait(false)).Select(i => i.IsSuccess).ToArray();

        private async Task<List<UpdateItemOutcome>> UpdateItemsDetailedAsync<T, K>(List<(K item, K? oldItem)> items, NamedEntity? owner = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return [];

            List<UpdateItemOutcome> result = [];
            try
            {
                var collectionEndpoint = EndpointResolver.ResolveEndpoint<T>(owner).TrimEnd('/');
                foreach (var pair in items)
                {
                    var endpoint = $"{collectionEndpoint}/{Uri.EscapeDataString(pair.oldItem!.Name)}";
                    var currentEntity = await LoadEntityByEndpointAsync<K>(endpoint, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (currentEntity == null)
                    {
                        m_logger.LogError("Failed to load {TypeName} from {Endpoint}", typeof(K).Name, endpoint);
                        result.Add(new UpdateItemOutcome(false));
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
                            result.Add(new UpdateItemOutcome(false, (int)response.StatusCode, message));
                        }
                        else
                        {
                            var updateMessage = await TryDeserializeUpdateMessageAsync(response, cancellationToken).ConfigureAwait(false);
                            if (updateMessage?.NotApplied != null && updateMessage.NotApplied.Count > 0)
                            {
                                var notApplied = updateMessage.NotApplied.Keys.ToList();
                                m_logger.LogError("Partial update detected for {TypeName} on {Endpoint}. Not applied properties: {NotApplied}", typeof(T).Name, endpoint, notApplied);
                                result.Add(new UpdateItemOutcome(false, updateMessage.ResponseStatusCode, updateMessage.Message, notApplied));
                            }
                            else
                            {
                                result.Add(new UpdateItemOutcome(true, (int)response.StatusCode, updateMessage?.Message));
                            }
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
                result.AddRange(Enumerable.Repeat(new UpdateItemOutcome(false), items.Count - result.Count));

            return result;
        }
        #endregion

        #region Insert
        /// <summary>
        /// Inserts an item in the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the insert was successful.</returns>
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
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the insert was successful.</returns>
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
        /// <returns>A task that represents the asynchronous operation. The task result contains an array of booleans indicating whether the each insert was successful.</returns>
        public async Task<bool[]> InsertItemsAsync<T, K>(List<K> items, int pageSize = 10, NamedEntity? owner = null, CancellationToken cancellationToken = default)
         where T : EntityCollection<K>
         where K : NamedEntity, new()
            => (await InsertItemsDetailedAsync<T, K>(items, pageSize, owner, cancellationToken).ConfigureAwait(false)).Select(i => i.IsSuccess).ToArray();

        private async Task<List<InsertItemOutcome>> InsertItemsDetailedAsync<T, K>(List<K> items, int pageSize = 10, NamedEntity? owner = null, CancellationToken cancellationToken = default)
         where T : EntityCollection<K>
         where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return [];

            var result = new InsertItemOutcome?[items.Count];

            try
            {
                var endpoint = EndpointResolver.ResolveEndpoint<T>(owner);

                var supportedItems = new List<(int index, K item)>();
                var unsupportedItems = new List<K>();
                var unsupportedMessage = "Unsupported driver detected for insert.";


                if (typeof(K) == typeof(Channel) || typeof(K) == typeof(Device))
                {
                    //check for usage of non supported drivers

                    // Load supported drivers from cache or API - prevents multiple calls to the docs endpoint
                    if (m_cachedSupportedDrivers == null)
                    {
                        m_cachedSupportedDrivers = await GetSupportedDriversAsync(cancellationToken).ConfigureAwait(false);
                    }

                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        var driver = item.GetDynamicProperty<string>(Properties.Channel.DeviceDriver);
                        var isSupported = !string.IsNullOrEmpty(driver) && m_cachedSupportedDrivers.ContainsKey(driver);
                        if (isSupported)
                        {
                            supportedItems.Add((i, item));
                        }
                        else
                        {
                            unsupportedItems.Add(item);
                            result[i] = new InsertItemOutcome(false, (int)HttpStatusCode.BadRequest, unsupportedMessage);
                        }
                    }

                    if (unsupportedItems.Count > 0)
                    {
                        m_logger.LogWarning("The following {NumItems} {TypeName} have unsupported drivers ({ListOfUsedUnsupportedDrivers}) and will not be inserted: {ItemsNames}",
                            unsupportedItems.Count, typeof(K).Name, unsupportedItems.Select(i => i.GetDynamicProperty<string>(Properties.Channel.DeviceDriver)).Distinct(), unsupportedItems.Select(i => i.Name));
                    }
                }
                else
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        supportedItems.Add((i, items[i]));
                    }
                }

                var totalPageCount = (int)Math.Ceiling((double)supportedItems.Count / pageSize);
                for (int i = 0; i < totalPageCount; i++)
                {
                    var pageItemMapping = supportedItems.Skip(i * pageSize).Take(pageSize).ToList();
                    var pageItems = pageItemMapping.Select(p => p.item).ToList();
                    m_logger.LogInformation("Inserting {NumItems} {TypeName}(s) on {Endpoint} in batch {BatchNr} of {TotalBatches} ...", pageItems.Count, typeof(K).Name, endpoint, i + 1, totalPageCount);

                    var jsonContent = JsonSerializer.Serialize(pageItems, KepJsonContext.GetJsonListTypeInfo<K>());
                    HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var response = await m_httpClient.PostAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        m_logger.LogError("Failed to insert {TypeName} from {Endpoint}: {ReasonPhrase}\n{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
                        foreach (var pageItem in pageItemMapping)
                        {
                            result[pageItem.index] = new InsertItemOutcome(false, (int)response.StatusCode, message);
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.MultiStatus)
                    {
                        // When a POST includes multiple objects, if one or more cannot be processed due to a parsing failure or 
                        // some other non - property validation error, the HTTPS status code 207(Multi - Status) will be returned along
                        // with a JSON object array containing the status for each object in the request.
                        var results = await JsonSerializer.DeserializeAsync(
                            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                            KepJsonContext.Default.ListApiResult, cancellationToken).ConfigureAwait(false) ?? [];

                        for (int entryIndex = 0; entryIndex < pageItemMapping.Count; entryIndex++)
                        {
                            var mappedItem = pageItemMapping[entryIndex];
                            if (entryIndex < results.Count)
                            {
                                var itemResult = results[entryIndex];
                                result[mappedItem.index] = new InsertItemOutcome(
                                    itemResult.IsSuccessStatusCode,
                                    itemResult.Code,
                                    itemResult.Message,
                                    itemResult.Property,
                                    itemResult.Description,
                                    itemResult.ErrorLine);
                            }
                            else
                            {
                                result[mappedItem.index] = new InsertItemOutcome(false, (int)HttpStatusCode.InternalServerError,
                                    "Multi-status response did not contain an entry for this item.");
                            }
                        }

                        var failedEntries = results?.Where(r => !r.IsSuccessStatusCode)?.ToList() ?? [];
                        m_logger.LogError("{NumSuccessFull} were successfull, failed to insert {NumFailed} {TypeName} from {Endpoint}: {ReasonPhrase}\nFailed:\n{Message}",
                            (results?.Count ?? 0) - failedEntries.Count, failedEntries.Count, typeof(T).Name, endpoint, response.ReasonPhrase, JsonSerializer.Serialize(failedEntries, KepJsonContext.Default.ListApiResult));
                    }
                    else
                    {
                        foreach (var pageItem in pageItemMapping)
                        {
                            result[pageItem.index] = new InsertItemOutcome(true, (int)response.StatusCode, response.ReasonPhrase);
                        }
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_kepwareApiClient.OnHttpRequestException(httpEx);
            }

            for (int i = 0; i < result.Length; i++)
            {
                result[i] ??= new InsertItemOutcome(false);
            }

            return result.Select(r => r!).ToList();
        }
        #endregion

        private async Task<UpdateApiResponseMessage?> TryDeserializeUpdateMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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

        private sealed record UpdateItemOutcome(bool IsSuccess, int? ResponseCode = null, string? ResponseMessage = null, IReadOnlyList<string>? NotAppliedProperties = null);

        private sealed record InsertItemOutcome(
            bool IsSuccess,
            int? ResponseCode = null,
            string? ResponseMessage = null,
            string? Property = null,
            string? Description = null,
            int? ErrorLine = null);

        #region Delete
        /// <summary>
        /// Deletes an item from the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the delete was successful.</returns>
        public Task<bool> DeleteItemAsync<T>(T item, CancellationToken cancellationToken = default)
          where T : NamedEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(item).TrimEnd('/');
            return DeleteItemByEndpointAsync<T>(endpoint, cancellationToken);
        }

        /// <summary>
        /// Deletes an item from the Kepware server using the item name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="itemName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the delete was successful.</returns>
        public Task<bool> DeleteItemAsync<T>(string itemName, CancellationToken cancellationToken = default)
         where T : NamedEntity, new()
         => DeleteItemAsync<T>([itemName], cancellationToken);
        public Task<bool> DeleteItemAsync<T>(string[] itemNames, CancellationToken cancellationToken = default)
         where T : NamedEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(itemNames).TrimEnd('/');
            return DeleteItemByEndpointAsync<T>(endpoint, cancellationToken);
        }

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
        /// Deletes an item from the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="item"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the delete was successful.</returns>
        public async Task<bool> DeleteItemAsync<T, K>(K item, NamedEntity? owner = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>
            where K : NamedEntity, new()
            => (await DeleteItemsAsync<T, K>([item], owner, cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        /// <summary>
        /// Deletes a list of items from the Kepware server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="items"></param>
        /// <param name="owner"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an array of booleans indicating whether each delete was successful.</returns>
        public async Task<bool[]> DeleteItemsAsync<T, K>(List<K> items, NamedEntity? owner = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>
            where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return [];

            List<bool> result = [];

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
                        result.Add(false);
                    }
                    else
                    {
                        result.Add(true);
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

        #region Load
        #region LoadEntity

        /// <summary>
        /// Loads an entity of type <typeparamref name="T"/> asynchronously by its name from the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity to load.</typeparam>
        /// <param name="name">The name of the entity to load. If null, loads the default entity.</param>
        /// <param name="query">Optional query parameters to append to the request URI.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The loaded entity of type <typeparamref name="T"/> or null if not found.</returns>
        public Task<T?> LoadEntityAsync<T>(string? name = default, IEnumerable<KeyValuePair<string, string?>>? query = null, CancellationToken cancellationToken = default)
            where T : BaseEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(string.IsNullOrEmpty(name) ? [] : [name]);
            endpoint = AppendQueryString(endpoint, query);
            var serializedRequest = query != null && query.Any(kv => kv.Key.Equals("content", StringComparison.OrdinalIgnoreCase) && kv.Value == "serialize");
            return LoadEntityByEndpointAsync<T>(endpoint, serialized: serializedRequest, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Loads an entity of type <typeparamref name="T"/> asynchronously by its owner from the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity to load.</typeparam>
        /// <param name="owner">The owner of the entity.</param>
        /// <param name="query">Optional query parameters to append to the request URI.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The loaded entity of type <typeparamref name="T"/> or null if not found.</returns>
        public Task<T?> LoadEntityAsync<T>(IEnumerable<string> owner, IEnumerable<KeyValuePair<string, string?>>? query = null, CancellationToken cancellationToken = default)
            where T : BaseEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(owner);
            endpoint = AppendQueryString(endpoint, query);
            var serializedRequest = query != null && query.Any(kv => kv.Key.Equals("content", StringComparison.OrdinalIgnoreCase) && kv.Value == "serialize");
            return LoadEntityByEndpointAsync<T>(endpoint, serialized: serializedRequest, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Loads an entity of type <typeparamref name="T"/> asynchronously by its name and owner from the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity to load.</typeparam>
        /// <param name="name">The name of the entity to load.</param>
        /// <param name="owner">The owner of the entity.</param>
        /// <param name="query">Optional query parameters to append to the request URI.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The loaded entity of type <typeparamref name="T"/> or null if not found.</returns>
        public async Task<T?> LoadEntityAsync<T>(string name, NamedEntity owner, IEnumerable<KeyValuePair<string, string?>>? query = null, CancellationToken cancellationToken = default)
            where T : BaseEntity, new()
        {
            var endpoint = EndpointResolver.ResolveEndpoint<T>(owner, name);
            endpoint = AppendQueryString(endpoint, query);

            var serializedRequest = query != null && query.Any(kv => kv.Key.Equals("content", StringComparison.OrdinalIgnoreCase) && kv.Value == "serialize");

            var entity = await LoadEntityByEndpointAsync<T>(endpoint, serialized: serializedRequest, cancellationToken: cancellationToken);

            if (entity is IHaveOwner ownable)
            {
                ownable.Owner = owner;
            }
            return entity;
        }

        protected internal async Task<T?> LoadEntityByEndpointAsync<T>(string endpoint, bool serialized = false, CancellationToken cancellationToken = default)
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
                var entity = default(T);

                if (serialized) 
                { 
                    entity = await DeserializeJsonLoadSerializedAsync<T>(response, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    entity = await DeserializeJsonAsync<T>(response, cancellationToken).ConfigureAwait(false);
                }

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

        /// <summary>
        /// Loads a collection of entities of type <typeparamref name="T"/> asynchronously by its owner from the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity collection to load.</typeparam>
        /// <param name="owner">The owner of the entity collection.</param>
        /// <param name="query">Optional query parameters to append to the request URI.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The loaded collection of entities of type <typeparamref name="T"/> or null if not found.</returns>
        public Task<T?> LoadCollectionAsync<T>(string? owner = default, IEnumerable<KeyValuePair<string, string?>>? query = null, CancellationToken cancellationToken = default)
             where T : EntityCollection<DefaultEntity>, new()
         => LoadCollectionAsync<T, DefaultEntity>(owner, query, cancellationToken);

        /// <summary>
        /// Loads a collection of entities of type <typeparamref name="T"/> asynchronously by its owner from the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity collection to load.</typeparam>
        /// <param name="owner">The owner of the entity collection.</param>
        /// <param name="query">Optional query parameters to append to the request URI.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The loaded collection of entities of type <typeparamref name="T"/> or null if not found.</returns>
        public Task<T?> LoadCollectionAsync<T>(NamedEntity owner, IEnumerable<KeyValuePair<string, string?>>? query = null, CancellationToken cancellationToken = default)
          where T : EntityCollection<DefaultEntity>, new()
         => LoadCollectionAsync<T, DefaultEntity>(owner, query, cancellationToken);

        /// <summary>
        /// Loads a collection of entities of type <typeparamref name="T"/> asynchronously by its owner from the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity collection to load.</typeparam>
        /// <typeparam name="K">The type of the entities in the collection.</typeparam>
        /// <param name="owner">The owner of the entity collection.</param>
        /// <param name="query">Optional query parameters to append to the request URI.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The loaded collection of entities of type <typeparamref name="T"/> or null if not found.</returns>
        public Task<T?> LoadCollectionAsync<T, K>(string? owner = default, IEnumerable<KeyValuePair<string, string?>>? query = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>, new()
            where K : BaseEntity, new()
            => LoadCollectionAsync<T, K>(string.IsNullOrEmpty(owner) ? [] : [owner], query, cancellationToken);

        /// <summary>
        /// Loads a collection of entities of type <typeparamref name="T"/> asynchronously by its owner from the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity collection to load.</typeparam>
        /// <typeparam name="K">The type of the entities in the collection.</typeparam>
        /// <param name="owner">The owner of the entity collection.</param>
        /// <param name="query">Optional query parameters to append to the request URI.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The loaded collection of entities of type <typeparamref name="T"/> or null if not found.</returns>
        public async Task<T?> LoadCollectionAsync<T, K>(NamedEntity owner, IEnumerable<KeyValuePair<string, string?>>? query = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>, new()
            where K : BaseEntity, new()
        {
            var collection = await LoadCollectionByEndpointAsync<T, K>(AppendQueryString(EndpointResolver.ResolveEndpoint<T>(owner), query), cancellationToken);
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

        /// <summary>
        /// Loads a collection of entities of type <typeparamref name="T"/> asynchronously by its endpoint from the Kepware server.
        /// </summary>
        /// <typeparam name="T">The type of the entity collection to load.</typeparam>
        /// <typeparam name="K">The type of the entities in the collection.</typeparam>
        /// <param name="owner">The owner of the entity collection.</param>
        /// <param name="query">Optional query parameters to append to the request URI.</param>
        /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
        /// <returns>The loaded collection of entities of type <typeparamref name="T"/> or null if not found.</returns>
        public Task<T?> LoadCollectionAsync<T, K>(IEnumerable<string> owner, IEnumerable<KeyValuePair<string, string?>>? query = null, CancellationToken cancellationToken = default)
            where T : EntityCollection<K>, new()
            where K : BaseEntity, new()
            => LoadCollectionByEndpointAsync<T, K>(AppendQueryString(EndpointResolver.ResolveEndpoint<T>(owner), query), cancellationToken);

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

        #endregion

        #region docs
        /// <summary>
        /// Returns a list of all supported drivers from the Kepware server.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ReadOnlyDictionary<string, Docs.Driver>> GetSupportedDriversAsync(CancellationToken cancellationToken = default)
        {
            if (m_cachedSupportedDrivers == null)
            {
                var drivers = await LoadSupportedDriversAsync(cancellationToken).ConfigureAwait(false);

                m_cachedSupportedDrivers = drivers.Where(d => !string.IsNullOrEmpty(d.DisplayName)).ToDictionary(d => d.DisplayName!).AsReadOnly();
            }
            return m_cachedSupportedDrivers;
        }

        /// <summary>
        /// Returns the channel properties for the specified driver in the Kepware server.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the device properties for the specified driver in the Kepware server.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Docs.Device> GetDevicePropertiesAsync(Docs.Driver driver, CancellationToken cancellationToken = default)
            => GetDevicePropertiesAsync(driver.DisplayName!, cancellationToken);

        /// <summary>
        /// Returns the device properties for the specified driver in the Kepware server.
        /// </summary>
        /// <param name="driverName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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

        #region private / internal methods

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

        /// <summary>
        /// Special deserialization method for responses from endpoints with `?content=serialize` that wrap the actual object in an additional 
        /// layer with the object name as the property name, e.g. `{ "Channel": { ... } }`. This is required to properly handle dynamic properties 
        /// on channels/devices/etc that conform to a different model from JSON type info for the base entity. This would cause  
        /// deserialization to fail if we tried to deserialize directly to the target type.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="httpResponse"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected Task<K?> DeserializeJsonLoadSerializedAsync<K>(HttpResponseMessage httpResponse, CancellationToken cancellationToken = default)
          where K : BaseEntity, new() => DeserializeJsonLoadSerializedAsync<K>(httpResponse, KepJsonContext.GetJsonTypeInfo<K>(), cancellationToken);

        /// <summary>
        /// Special deserialization method for responses from endpoints with `?content=serialize` that wrap the actual object in an additional 
        /// layer with the object name as the property name, e.g. `{ "Channel": { ... } }`. This is required to properly handle dynamic properties 
        /// on channels/devices/etc that conform to a different model from JSON type info for the base entity. This would cause  
        /// deserialization to fail if we tried to deserialize directly to the target type.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="httpResponse"></param>
        /// <param name="jsonTypeInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task<K?> DeserializeJsonLoadSerializedAsync<K>(HttpResponseMessage httpResponse, JsonTypeInfo<K> jsonTypeInfo, CancellationToken cancellationToken = default)
          where K : BaseEntity, new()
        {
            try
            {
                using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
                var wrapper = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(
                        stream, KepJsonContext.Default.DictionaryStringJsonElement, cancellationToken).ConfigureAwait(false)
                    ?? throw new JsonException("Response was not a JSON object.");

                var first = wrapper.Values.FirstOrDefault();
                if (first.ValueKind != JsonValueKind.Object)
                    throw new JsonException("Expected the first property to be a JSON object.");

                return first.Deserialize<K>(jsonTypeInfo: KepJsonContext.GetJsonTypeInfo<K>())
                       ?? throw new JsonException("Failed to deserialize channel object.");
                //return await JsonSerializer.DeserializeAsync(stream, jsonTypeInfo, cancellationToken);
            }
            catch (JsonException ex)
            {
                m_logger.LogError(ex, "JSON Deserialization failed");
                return default;
            }
        }


        #endregion

        /// <summary>
        /// Clears any internal caches (supported drivers, supported channels/devices).
        /// Called when the underlying connection is lost so subsequent calls re-fetch data.
        /// </summary>
        internal void InvalidateCaches()
        {
            // drop cached drivers so next call re-loads from /doc endpoint
            m_cachedSupportedDrivers = null;

            // clear cached channel/device property dictionaries
            m_cachedSupportedChannels.Clear();
            m_cachedSupportedDevices.Clear();
        }

        #endregion

        /// <summary>
        /// Append query parameters to an endpoint string. Encodes keys and values with Uri.EscapeDataString.
        /// Null or empty values are skipped. If `endpoint` already contains a query, parameters are appended with &amp;.
        /// </summary>
        private static string AppendQueryString(string endpoint, IEnumerable<KeyValuePair<string, string?>>? query)
        {
            if (query == null)
                return endpoint;

            var sb = new StringBuilder();
            foreach (var kv in query)
            {
                if (kv.Key == null)
                    continue;
                // Skip parameters with null values to match typical REST filter behavior.
                if (kv.Value is null)
                    continue;

                if (sb.Length > 0)
                    sb.Append('&');

                sb.Append(Uri.EscapeDataString(kv.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(kv.Value));
            }

            if (sb.Length == 0)
                return endpoint;

            return endpoint + (endpoint.Contains('?') ? "&" : "?") + sb.ToString();
        }
    }
}