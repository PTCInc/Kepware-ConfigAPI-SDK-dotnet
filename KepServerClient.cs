﻿using KepwareSync.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KepwareSync
{
    public class KepServerClient
    {
        private readonly ILogger<KepServerClient> m_logger;
        private readonly HttpClient m_httpClient;

        public KepServerClient(ILogger<KepServerClient> logger, HttpClient httpClient)
        {
            m_logger = logger;
            m_httpClient = httpClient;
        }

        public async Task<string> GetFullProjectAsync()
        {
            m_logger.LogInformation("Downloading full project from KepServer...");
            // Retrieve full project JSON from KepServer REST API
            return await Task.FromResult("{\"project\":\"example\"}"); // Placeholder
        }

        public async Task UpdateFullProjectAsync(string projectJson)
        {
            m_logger.LogInformation("Uploading full project to KepServer...");
            // Upload full project JSON to KepServer REST API
            await Task.CompletedTask;
        }

        /// <summary>
        /// Compares two collections of entities and applies the changes to the target collection.
        /// Left should represent the source and Right should represent the API (target).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="K"></typeparam>
        /// <param name="sourceCollection"></param>
        /// <param name="apiCollection"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public async Task<EntityCompare.CollectionResultBucket<T, K>> CompareAndApply<T, K>(T? sourceCollection, T? apiCollection, NamedEntity? owner = null)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
        {
            var compareResult = EntityCompare.Compare<T, K>(sourceCollection, apiCollection);

            /// This are the items that are in the API but not in the source
            /// --> we need to delete them
            await DeleteItemsAsync<T, K>(compareResult.ItemsOnlyInRight.Select(i => i.Right!).ToList(), owner);

            /// This are the items both in the API and the source
            /// --> we need to update them
            await UpdateItemsAsync<T, K>(compareResult.ChangedItems.Select(i => (i.Left!, i.Right)).ToList(), owner);

            /// This are the items that are in the source but not in the API
            /// --> we need to insert them
            await InsertItemsAsync<T, K>(compareResult.ItemsOnlyInLeft.Select(i => i.Left!).ToList(), owner);

            return compareResult;
        }


        public async Task UpdateItemAsync<T>(T item, T? oldItem = default)
           where T : NamedEntity, new()
        {
            var endpoint = ResolveEndpoint<T>(oldItem ?? item);

            m_logger.LogInformation("Updating {TypeName} on {Endpoint}...", typeof(T).Name, endpoint);

            var currentEntity = await LoadEntityAsync<T>(oldItem ?? item);
            item.ProjectId = currentEntity?.ProjectId;

            HttpContent httpContent = new StringContent(JsonSerializer.Serialize(item, KepJsonContext.GetJsonTypeInfo<T>()), Encoding.UTF8, "application/json");
            var response = await m_httpClient.PutAsync(endpoint, httpContent);
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                m_logger.LogError("Failed to update {TypeName} from {Endpoint}: {ReasonPhrase}\n\t{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
            }
        }

        public Task UpdateItemAsync<T, K>(K item, K? oldItem = default, NamedEntity? owner = null)
            where T : EntityCollection<K>
          where K : NamedEntity, new()
            => UpdateItemsAsync<T, K>([(item, oldItem)], owner);
        public async Task UpdateItemsAsync<T, K>(List<(K item, K? oldItem)> items, NamedEntity? owner = null)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return;
            var collectionEndpoint = ResolveEndpoint<T>(owner).TrimEnd('/');
            foreach (var pair in items)
            {
                var endpoint = $"{collectionEndpoint}/{pair.item.Name}";
                var currentEntity = await LoadEntityAsync<K>(endpoint, owner);
                pair.item.ProjectId = currentEntity?.ProjectId;

                var diff = pair.item.GetUpdateDiff(currentEntity!);

                m_logger.LogInformation("Updating {TypeName} on {Endpoint}, values {Diff}", typeof(T).Name, endpoint, diff);

                HttpContent httpContent = new StringContent(JsonSerializer.Serialize(diff, KepJsonContext.Default.DictionaryStringJsonElement), Encoding.UTF8, "application/json");
                var response = await m_httpClient.PutAsync(endpoint, httpContent);
                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    m_logger.LogError("Failed to update {TypeName} from {Endpoint}: {ReasonPhrase}\n\t{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
                }
            }
        }

        public Task InsertItemAsync<T, K>(K item, NamedEntity? owner = null)
          where T : EntityCollection<K>
          where K : NamedEntity, new()
            => InsertItemsAsync<T, K>([item], owner);

        public async Task InsertItemsAsync<T, K>(List<K> items, NamedEntity? owner = null)
            where T : EntityCollection<K>
            where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return;

            var endpoint = ResolveEndpoint<T>(owner);
            m_logger.LogInformation("Inserting {TypeName} on {Endpoint}...", typeof(K).Name, endpoint);

            HttpContent httpContent = new StringContent(JsonSerializer.Serialize(items, KepJsonContext.GetJsonListTypeInfo<K>()), Encoding.UTF8, "application/json");
            var response = await m_httpClient.PostAsync(endpoint, httpContent);
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                m_logger.LogError("Failed to insert {TypeName} from {Endpoint}: {ReasonPhrase}\n\t{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
            }
        }

        public Task DeleteItemAsync<T, K>(K item, NamedEntity? owner = null)
            where T : EntityCollection<K>
            where K : NamedEntity, new()
            => DeleteItemsAsync<T, K>([item], owner);

        public async Task DeleteItemsAsync<T, K>(List<K> items, NamedEntity? owner = null)
            where T : EntityCollection<K>
            where K : NamedEntity, new()
        {
            if (items.Count == 0)
                return;
            var collectionEndpoint = ResolveEndpoint<T>(owner).TrimEnd('/');
            foreach (var item in items)
            {
                var endpoint = $"{collectionEndpoint}/{item.Name}";

                m_logger.LogInformation("Deleting {TypeName} on {Endpoint}...", typeof(K).Name, endpoint);

                var response = await m_httpClient.DeleteAsync(endpoint);
                if (!response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync();
                    m_logger.LogError("Failed to delete {TypeName} from {Endpoint}: {ReasonPhrase}\n\t{Message}", typeof(T).Name, endpoint, response.ReasonPhrase, message);
                }
            }
        }

        public async Task<Project> LoadProject(bool blnDeep = true)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var project = await LoadEntityAsync<Project>();

            if (project == null)
            {
                m_logger.LogWarning("Failed to load project");
                return new Project();
            }
            else
            {
                project.Channels = await LoadCollectionAsync<ChannelCollection, Channel>();

                if (blnDeep && project.Channels != null)
                {
                    int totalChannelCount = project.Channels.Items.Count;
                    int loadedChannelCount = 0;
                    await Task.WhenAll(project.Channels.Select(async channel =>
                    {
                        channel.Devices = await LoadCollectionAsync<DeviceCollection, Device>(channel);

                        if (channel.Devices != null)
                        {
                            await Task.WhenAll(channel.Devices.Select(async device =>
                            {
                                device.Tags = await LoadCollectionAsync<DeviceTagCollection, Tag>(device);
                                device.TagGroups = await LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device);

                                if (device.TagGroups != null)
                                {
                                    await Task.WhenAll(device.TagGroups.Select(async tagGroup =>
                                    {
                                        tagGroup.Tags = await LoadCollectionAsync<DeviceTagGroupTagCollection, Tag>(tagGroup);
                                    }));
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

                return project;
            }
        }

        public Task<T?> LoadEntityAsync<T>(NamedEntity? owner = null)
          where T : BaseEntity, new()
        {

            var endpoint = ResolveEndpoint<T>(owner);
            return LoadEntityAsync<T>(endpoint, owner);
        }

        private async Task<T?> LoadEntityAsync<T>(string endpoint, NamedEntity? owner = null)
          where T : BaseEntity, new()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            m_logger.LogDebug("Loading {TypeName} from {Endpoint}...", typeof(T).Name, endpoint);
            var response = await m_httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                m_logger.LogError("Failed to load {TypeName} from {Endpoint}: {ReasonPhrase}", typeof(T).Name, endpoint, response.ReasonPhrase);
                return default;
            }

            var entity = await DeserializeJsonAsync<T>(response);
            if (entity != null && entity is IHaveOwner ownable)
            {
                ownable.Owner = owner;
            }

            return entity;
        }


        public Task<T?> LoadCollectionAsync<T>(NamedEntity? owner = null)
          where T : EntityCollection<DefaultEntity>, new()
         => LoadCollectionAsync<T, DefaultEntity>(owner);

        public async Task<T?> LoadCollectionAsync<T, K>(NamedEntity? owner = null)
            where T : EntityCollection<K>, new()
            where K : BaseEntity, new()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var endpoint = ResolveEndpoint<T>(owner);

            m_logger.LogDebug("Loading {TypeName} from {Endpoint}...", typeof(T).Name, endpoint);
            var response = await m_httpClient.GetAsync(endpoint);
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

                var resultCollection = new T() { Owner = owner, Items = collection };
                return resultCollection;
            }
            else
            {
                m_logger.LogError("Failed to deserialize {TypeName} from {Endpoint}", typeof(T).Name, endpoint);
                return default;
            }
        }

        protected async Task<K?> DeserializeJsonAsync<K>(HttpResponseMessage httpResponse)
          where K : BaseEntity, new()
        {
            try
            {
                using (var stream = await httpResponse.Content.ReadAsStreamAsync())
                    return await JsonSerializer.DeserializeAsync(stream, KepJsonContext.GetJsonTypeInfo<K>());
            }
            catch (JsonException ex)
            {
                m_logger.LogError("JSON Deserialization failed: {Message}", ex.Message);
                return null;
            }
        }


        protected async Task<List<K>?> DeserializeJsonArrayAsync<K>(HttpResponseMessage httpResponse)
            where K : BaseEntity, new()
        {
            try
            {
                using (var stream = await httpResponse.Content.ReadAsStreamAsync())
                    return await JsonSerializer.DeserializeAsync(stream, KepJsonContext.GetJsonListTypeInfo<K>());
            }
            catch (JsonException ex)
            {
                m_logger.LogError("JSON Deserialization failed: {Message}", ex.Message);
                return null;
            }
        }

        private string ResolveEndpoint<T>(NamedEntity? owner)
        {
            var endpointTemplate = typeof(T).GetCustomAttributes(typeof(EndpointAttribute), false)
                .OfType<EndpointAttribute>()
                .FirstOrDefault()?.EndpointTemplate;

            if (endpointTemplate == null)
            {
                throw new InvalidOperationException($"No endpoint defined for {typeof(T).Name}");
            }

            // Regex to find all placeholders in the endpoint template
            var placeholders = Regex.Matches(endpointTemplate, @"\{(.+?)\}")
                .Reverse();

            // owner -> owner.Owner -> owner.Owner.Owner -> ... to replace the placeholders in the endpoint template by reverse order

            foreach (Match placeholder in placeholders)
            {
                var placeholderName = placeholder.Groups[1].Value;

                string? placeholderValue = owner?.Name;
                if (!string.IsNullOrEmpty(placeholderValue))
                {
                    endpointTemplate = endpointTemplate.Replace(placeholder.Value, Uri.EscapeDataString(placeholderValue));

                    if (owner is IHaveOwner ownable && ownable.Owner != null)
                        owner = ownable.Owner;
                    else
                        break;
                }
                else
                {
                    throw new InvalidOperationException($"Placeholder '{placeholderName}' in endpoint template '{endpointTemplate}' could not be resolved.");
                }
            }

            return endpointTemplate;
        }


    }
}
