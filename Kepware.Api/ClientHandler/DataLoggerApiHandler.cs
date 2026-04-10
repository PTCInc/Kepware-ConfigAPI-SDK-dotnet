using Kepware.Api.Model;
using Kepware.Api.Model.Services;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Kepware.Api.ClientHandler
{
    /// <summary>
    /// Handles operations related to DataLogger log group configurations in the Kepware server.
    /// Supports log group CRUD operations and their child log items, column mappings, and triggers.
    /// </summary>
    public class DataLoggerApiHandler
    {
        private readonly KepwareApiClient m_kepwareApiClient;
        private readonly ILogger<DataLoggerApiHandler> m_logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoggerApiHandler"/> class.
        /// </summary>
        /// <param name="kepwareApiClient">The Kepware Configuration API client.</param>
        /// <param name="logger">The logger instance.</param>
        public DataLoggerApiHandler(KepwareApiClient kepwareApiClient, ILogger<DataLoggerApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_logger = logger;
        }

        #region Log Group

        /// <summary>
        /// Gets a log group with the specified name.
        /// </summary>
        /// <param name="name">The name of the log group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="LogGroup"/> or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task<LogGroup?> GetLogGroupAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Log group name cannot be null or empty", nameof(name));

            return await m_kepwareApiClient.GenericConfig.LoadEntityAsync<LogGroup>(name, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets all log groups.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="LogGroupCollection"/>, or null if none exist.</returns>
        public async Task<LogGroupCollection?> GetLogGroupsAsync(CancellationToken cancellationToken = default)
        {
            return await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<LogGroupCollection, LogGroup>(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets or creates a log group with the specified name.
        /// If the group exists, it is loaded and returned. If it does not exist, it is created with the specified properties.
        /// </summary>
        /// <param name="name">The name of the log group.</param>
        /// <param name="properties">Optional properties to set on the log group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created or loaded <see cref="LogGroup"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the log group cannot be created or loaded.</exception>
        public async Task<LogGroup> GetOrCreateLogGroupAsync(string name, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Log group name cannot be null or empty", nameof(name));

            var group = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<LogGroup>(name, cancellationToken: cancellationToken);

            if (group == null)
            {
                group = await CreateLogGroupAsync(name, properties, cancellationToken: cancellationToken);
                if (group == null)
                {
                    throw new InvalidOperationException($"Failed to create or load log group '{name}'");
                }
            }

            return group;
        }

        /// <summary>
        /// Creates a new log group with the specified name.
        /// </summary>
        /// <param name="name">The name of the log group.</param>
        /// <param name="properties">Optional properties to set on the log group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created <see cref="LogGroup"/>, or null if creation failed.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task<LogGroup?> CreateLogGroupAsync(string name, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Log group name cannot be null or empty", nameof(name));

            var group = new LogGroup(name);
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    group.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<LogGroupCollection, LogGroup>(group, cancellationToken: cancellationToken))
            {
                return group;
            }

            return null;
        }

        /// <summary>
        /// Updates the specified log group.
        /// When <paramref name="autoDisable"/> is true and the group is currently enabled,
        /// the group is temporarily disabled before the update and re-enabled afterward.
        /// </summary>
        /// <param name="group">The log group to update.</param>
        /// <param name="autoDisable">
        /// When true, disables the group before updating and re-enables it after updating
        /// if it was originally enabled.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the update was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the group is null.</exception>
        public Task<bool> UpdateLogGroupAsync(LogGroup group, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(group);
            return WithAutoDisableAsync(group, autoDisable,
                () => m_kepwareApiClient.GenericConfig.UpdateItemAsync(group, oldItem: null, cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Deletes the specified log group.
        /// </summary>
        /// <param name="group">The log group to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the group is null.</exception>
        public Task<bool> DeleteLogGroupAsync(LogGroup group, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(group);
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync(group, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes the log group with the specified name.
        /// </summary>
        /// <param name="name">The name of the log group to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public Task<bool> DeleteLogGroupAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Log group name cannot be null or empty", nameof(name));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync<LogGroup>(name, cancellationToken: cancellationToken);
        }

        #endregion

        #region Log Item

        /// <summary>
        /// Gets a log item with the specified name from the given parent log group.
        /// </summary>
        /// <param name="name">The name of the log item.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="LogItem"/> or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public async Task<LogItem?> GetLogItemAsync(string name, LogGroup parent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Log item name cannot be null or empty", nameof(name));
            ArgumentNullException.ThrowIfNull(parent);

            return await m_kepwareApiClient.GenericConfig.LoadEntityAsync<LogItem>(name, parent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets all log items from the given parent log group.
        /// </summary>
        /// <param name="parent">The parent log group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="LogItemCollection"/>, or null if none exist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public async Task<LogItemCollection?> GetLogItemsAsync(LogGroup parent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(parent);
            return await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<LogItemCollection, LogItem>(parent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets or creates a log item with the specified name under the given parent log group.
        /// If the item exists, it is loaded and returned. If it does not exist, it is created.
        /// </summary>
        /// <param name="name">The name of the log item.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="properties">Optional properties to set on the log item.</param>
        /// <param name="autoDisable">When true, disables the parent group before mutating and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created or loaded <see cref="LogItem"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the log item cannot be created or loaded.</exception>
        public async Task<LogItem> GetOrCreateLogItemAsync(string name, LogGroup parent, IDictionary<string, object>? properties = null, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Log item name cannot be null or empty", nameof(name));
            ArgumentNullException.ThrowIfNull(parent);

            var item = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<LogItem>(name, parent, cancellationToken: cancellationToken);

            if (item == null)
            {
                item = await CreateLogItemAsync(name, parent, properties, autoDisable, cancellationToken);
                if (item == null)
                    throw new InvalidOperationException($"Failed to create or load log item '{name}'");
            }

            return item;
        }

        /// <summary>
        /// Creates a new log item with the specified name under the given parent log group.
        /// </summary>
        /// <param name="name">The name of the log item.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="properties">Optional properties to set on the log item.</param>
        /// <param name="autoDisable">When true, disables the parent group before creating and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created <see cref="LogItem"/>, or null if creation failed.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public async Task<LogItem?> CreateLogItemAsync(string name, LogGroup parent, IDictionary<string, object>? properties = null, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Log item name cannot be null or empty", nameof(name));
            ArgumentNullException.ThrowIfNull(parent);

            return await WithAutoDisableAsync(parent, autoDisable, async () =>
            {
                var item = new LogItem(name) { Owner = parent };
                if (properties != null)
                {
                    foreach (var property in properties)
                        item.SetDynamicProperty(property.Key, property.Value);
                }

                if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<LogItemCollection, LogItem>(item, parent, cancellationToken: cancellationToken))
                    return item;

                return null;
            }, cancellationToken);
        }

        /// <summary>
        /// Updates the specified log item.
        /// </summary>
        /// <param name="item">The log item to update.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="autoDisable">When true, disables the parent group before updating and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the update was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the item or parent is null.</exception>
        public Task<bool> UpdateLogItemAsync(LogItem item, LogGroup parent, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(parent);
            return WithAutoDisableAsync(parent, autoDisable,
                () => m_kepwareApiClient.GenericConfig.UpdateItemAsync(item, oldItem: null, cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Deletes the specified log item.
        /// </summary>
        /// <param name="item">The log item to delete.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="autoDisable">When true, disables the parent group before deleting and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the item or parent is null.</exception>
        public Task<bool> DeleteLogItemAsync(LogItem item, LogGroup parent, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(parent);
            return WithAutoDisableAsync(parent, autoDisable,
                () => m_kepwareApiClient.GenericConfig.DeleteItemAsync(item, cancellationToken: cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Deletes the log item with the specified name from the given parent log group.
        /// </summary>
        /// <param name="name">The name of the log item to delete.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="autoDisable">When true, disables the parent group before deleting and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public Task<bool> DeleteLogItemAsync(string name, LogGroup parent, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Log item name cannot be null or empty", nameof(name));
            ArgumentNullException.ThrowIfNull(parent);
            return WithAutoDisableAsync(parent, autoDisable,
                () => m_kepwareApiClient.GenericConfig.DeleteItemAsync(new LogItem(name) { Owner = parent }, cancellationToken),
                cancellationToken);
        }

        #endregion

        #region Column Mapping

        /// <summary>
        /// Gets all column mappings from the given parent log group.
        /// Column mappings are auto-generated by the server and cannot be created or deleted via the API.
        /// </summary>
        /// <param name="parent">The parent log group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="ColumnMappingCollection"/>, or null if none exist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public async Task<ColumnMappingCollection?> GetColumnMappingsAsync(LogGroup parent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(parent);
            return await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<ColumnMappingCollection, ColumnMapping>(parent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets a column mapping with the specified name from the given parent log group.
        /// </summary>
        /// <param name="name">The name of the column mapping.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="ColumnMapping"/> or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public async Task<ColumnMapping?> GetColumnMappingAsync(string name, LogGroup parent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Column mapping name cannot be null or empty", nameof(name));
            ArgumentNullException.ThrowIfNull(parent);
            return await m_kepwareApiClient.GenericConfig.LoadEntityAsync<ColumnMapping>(name, parent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Updates the specified column mapping.
        /// Column mappings are auto-generated by the server; only existing mappings may be modified.
        /// </summary>
        /// <param name="mapping">The column mapping to update.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="autoDisable">When true, disables the parent group before updating and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the update was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the mapping or parent is null.</exception>
        public Task<bool> UpdateColumnMappingAsync(ColumnMapping mapping, LogGroup parent, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mapping);
            ArgumentNullException.ThrowIfNull(parent);
            return WithAutoDisableAsync(parent, autoDisable,
                () => m_kepwareApiClient.GenericConfig.UpdateItemAsync(mapping, oldItem: null, cancellationToken),
                cancellationToken);
        }

        #endregion

        #region Triggers

        /// <summary>
        /// Gets a trigger with the specified name from the given parent log group.
        /// </summary>
        /// <param name="name">The name of the trigger.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="Trigger"/> or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public async Task<Trigger?> GetTriggerAsync(string name, LogGroup parent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Trigger name cannot be null or empty", nameof(name));
            ArgumentNullException.ThrowIfNull(parent);
            return await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Trigger>(name, parent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets all triggers from the given parent log group.
        /// </summary>
        /// <param name="parent">The parent log group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="TriggerCollection"/>, or null if none exist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public async Task<TriggerCollection?> GetTriggersAsync(LogGroup parent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(parent);
            return await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<TriggerCollection, Trigger>(parent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Gets or creates a trigger with the specified name under the given parent log group.
        /// If the trigger exists, it is loaded and returned. If it does not exist, it is created.
        /// </summary>
        /// <param name="name">The name of the trigger.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="properties">Optional properties to set on the trigger.</param>
        /// <param name="autoDisable">When true, disables the parent group before mutating and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created or loaded <see cref="Trigger"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the trigger cannot be created or loaded.</exception>
        public async Task<Trigger> GetOrCreateTriggerAsync(string name, LogGroup parent, IDictionary<string, object>? properties = null, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Trigger name cannot be null or empty", nameof(name));
            ArgumentNullException.ThrowIfNull(parent);

            var trigger = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Trigger>(name, parent, cancellationToken: cancellationToken);

            if (trigger == null)
            {
                trigger = await CreateTriggerAsync(name, parent, properties, autoDisable, cancellationToken);
                if (trigger == null)
                    throw new InvalidOperationException($"Failed to create or load trigger '{name}'");
            }

            return trigger;
        }

        /// <summary>
        /// Creates a new trigger with the specified name under the given parent log group.
        /// </summary>
        /// <param name="name">The name of the trigger.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="properties">Optional properties to set on the trigger.</param>
        /// <param name="autoDisable">When true, disables the parent group before creating and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created <see cref="Trigger"/>, or null if creation failed.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public async Task<Trigger?> CreateTriggerAsync(string name, LogGroup parent, IDictionary<string, object>? properties = null, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Trigger name cannot be null or empty", nameof(name));
            ArgumentNullException.ThrowIfNull(parent);

            return await WithAutoDisableAsync(parent, autoDisable, async () =>
            {
                var trigger = new Trigger(name) { Owner = parent };
                if (properties != null)
                {
                    foreach (var property in properties)
                        trigger.SetDynamicProperty(property.Key, property.Value);
                }

                if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<TriggerCollection, Trigger>(trigger, parent, cancellationToken: cancellationToken))
                    return trigger;

                return null;
            }, cancellationToken);
        }

        /// <summary>
        /// Updates the specified trigger.
        /// </summary>
        /// <param name="trigger">The trigger to update.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="autoDisable">When true, disables the parent group before updating and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the update was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the trigger or parent is null.</exception>
        public Task<bool> UpdateTriggerAsync(Trigger trigger, LogGroup parent, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(trigger);
            ArgumentNullException.ThrowIfNull(parent);
            return WithAutoDisableAsync(parent, autoDisable,
                () => m_kepwareApiClient.GenericConfig.UpdateItemAsync(trigger, oldItem: null, cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Deletes the specified trigger.
        /// </summary>
        /// <param name="trigger">The trigger to delete.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="autoDisable">When true, disables the parent group before deleting and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the trigger or parent is null.</exception>
        public Task<bool> DeleteTriggerAsync(Trigger trigger, LogGroup parent, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(trigger);
            ArgumentNullException.ThrowIfNull(parent);
            return WithAutoDisableAsync(parent, autoDisable,
                () => m_kepwareApiClient.GenericConfig.DeleteItemAsync(trigger, cancellationToken: cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Deletes the trigger with the specified name from the given parent log group.
        /// </summary>
        /// <param name="name">The name of the trigger to delete.</param>
        /// <param name="parent">The parent log group.</param>
        /// <param name="autoDisable">When true, disables the parent group before deleting and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent is null.</exception>
        public Task<bool> DeleteTriggerAsync(string name, LogGroup parent, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Trigger name cannot be null or empty", nameof(name));
            ArgumentNullException.ThrowIfNull(parent);
            return WithAutoDisableAsync(parent, autoDisable,
                () => m_kepwareApiClient.GenericConfig.DeleteItemAsync(new Trigger(name) { Owner = parent }, cancellationToken),
                cancellationToken);
        }

        #endregion

        #region ResetColumnMapping

        /// <summary>
        /// Initiates the ResetColumnMapping service for the specified log group,
        /// using a default time-to-live of 30 seconds.
        /// </summary>
        /// <param name="group">The log group whose column mappings should be reset.</param>
        /// <param name="autoDisable">When true, disables the log group before the operation and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="KepServerJobPromise"/> representing the async server job.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the group is null.</exception>
        public Task<KepServerJobPromise> ResetColumnMappingAsync(LogGroup group, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(group);
            return ResetColumnMappingAsync(group, TimeSpan.FromSeconds(30), autoDisable, cancellationToken);
        }

        /// <summary>
        /// Initiates the ResetColumnMapping service for the specified log group.
        /// </summary>
        /// <param name="group">The log group whose column mappings should be reset.</param>
        /// <param name="timeToLive">The job's desired time-to-live (1–300 seconds).</param>
        /// <param name="autoDisable">When true, disables the log group before the operation and re-enables it after.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="KepServerJobPromise"/> representing the async server job.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the group is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timeToLive is outside the range 1–300 seconds.</exception>
        public async Task<KepServerJobPromise> ResetColumnMappingAsync(LogGroup group, TimeSpan timeToLive, bool autoDisable = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(group);

            if (timeToLive.TotalSeconds < 1)
                throw new ArgumentOutOfRangeException(nameof(timeToLive), "Time to live must be at least 1 second");
            if (timeToLive.TotalSeconds > 300)
                throw new ArgumentOutOfRangeException(nameof(timeToLive), "Time to live must be at most 300 seconds");

            var endpoint = $"/config/v1/project/_datalogger/log_groups/{group.Name}/services/ResetColumnMapping";

            return await WithAutoDisableAsync(group, autoDisable, async () =>
            {
                var request = new ServiceInvocationRequest { TimeToLiveSeconds = (int)timeToLive.TotalSeconds };
                HttpContent httpContent = new StringContent(
                    JsonSerializer.Serialize(request, KepJsonContext.Default.ServiceInvocationRequest),
                    Encoding.UTF8, "application/json");

                var response = await m_kepwareApiClient.HttpClient.PutAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);
                var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return new KepServerJobPromise(endpoint, timeToLive,
                        (ApiResponseCode)(int)response.StatusCode,
                        $"ResetColumnMapping request failed with status code {(ApiResponseCode)(int)response.StatusCode} and message: {message}");

                try
                {
                    var jobResponse = JsonSerializer.Deserialize<JobResponseMessage>(message, KepJsonContext.Default.JobResponseMessage);
                    return jobResponse != null
                        ? new KepServerJobPromise(endpoint, timeToLive, jobResponse, m_kepwareApiClient.HttpClient)
                        : new KepServerJobPromise(endpoint, timeToLive, (ApiResponseCode)(int)response.StatusCode, "Failed to deserialize response message");
                }
                catch (JsonException jex)
                {
                    return new KepServerJobPromise(endpoint, timeToLive, (ApiResponseCode)(int)response.StatusCode, jex.Message);
                }
            }, cancellationToken);
        }

        #endregion

        #region CompareAndApply

        /// <summary>
        /// Compares a source <see cref="DataLoggerContainer"/> against the current server state and applies
        /// the minimum set of changes required to make the server match the source.
        /// </summary>
        /// <remarks>
        /// Each log group is processed fully end-to-end before moving to the next, minimising the time
        /// any individual group spends in a disabled state. Changes within each group are applied in
        /// mandatory order:
        /// <list type="number">
        ///   <item><description>Log group properties (changed groups only)</description></item>
        ///   <item><description>Log items (add / remove / update)</description></item>
        ///   <item><description>Column mappings (update only — never created or deleted)</description></item>
        ///   <item><description>Triggers (add / remove / update)</description></item>
        /// </list>
        /// When <paramref name="autoDisable"/> is <see langword="true"/>, any log group that is currently
        /// enabled — whether its own properties changed or only its children changed — is disabled before
        /// its changes begin and re-enabled after all four sub-steps complete.
        /// </remarks>
        /// <param name="source">The desired target state as a <see cref="DataLoggerContainer"/>.</param>
        /// <param name="current">The current server state as a <see cref="DataLoggerContainer"/>.</param>
        /// <param name="autoDisable">
        /// When <see langword="true"/>, any enabled log group that has changes (group-level or child-level)
        /// is temporarily disabled before changes begin and re-enabled afterward.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectCompareAndApplyResult"/> summarising the inserts, updates, deletes, and
        /// any failures encountered during the apply pass.
        /// </returns>
        public async Task<ProjectCompareAndApplyResult> CompareAndApplyAsync(
            DataLoggerContainer? source,
            DataLoggerContainer? current,
            bool autoDisable = false,
            CancellationToken cancellationToken = default)
        {
            var result = new ProjectCompareAndApplyResult();

            // Pure in-memory diff — no HTTP calls.
            var groupDiff = EntityCompare.Compare<LogGroupCollection, LogGroup>(
                source?.LogGroups, current?.LogGroups);

            // Delete log groups that exist only in current (removed from source).
            foreach (var bucket in groupDiff.ItemsOnlyInRight)
            {
                if (await m_kepwareApiClient.GenericConfig
                        .DeleteItemAsync(bucket.Right!, cancellationToken: cancellationToken)
                        .ConfigureAwait(false))
                    result.AddDeleteSuccess();
                else
                    result.AddFailure(new ApplyFailure
                    {
                        Operation = ApplyOperation.Delete,
                        AttemptedItem = bucket.Right!,
                    });
            }

            // Insert log groups that exist only in source (new to current).
            foreach (var bucket in groupDiff.ItemsOnlyInLeft)
            {
                if (await m_kepwareApiClient.GenericConfig
                        .InsertItemAsync<LogGroupCollection, LogGroup>(bucket.Left!, cancellationToken: cancellationToken)
                        .ConfigureAwait(false))
                    result.AddInsertSuccess();
                else
                    result.AddFailure(new ApplyFailure
                    {
                        Operation = ApplyOperation.Insert,
                        AttemptedItem = bucket.Left!,
                    });
            }

            // Unchanged groups: children may still have changes; apply with optional auto-disable.
            foreach (var bucket in groupDiff.UnchangedItems)
            {
                await ApplyGroupWithChildrenAsync(
                    bucket.Left!, bucket.Right!, groupChanged: false, autoDisable, result, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Changed groups: update group properties + children; apply with optional auto-disable.
            foreach (var bucket in groupDiff.ChangedItems)
            {
                await ApplyGroupWithChildrenAsync(
                    bucket.Left!, bucket.Right!, groupChanged: true, autoDisable, result, cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// Processes a single log group end-to-end: optionally disables it, applies group property
        /// changes (if any), applies all child collections in order, then re-enables it.
        /// </summary>
        /// <remarks>
        /// For <paramref name="groupChanged"/> groups the group property update is folded into the same
        /// PUT that acts as the disable when <paramref name="autoDisable"/> is <see langword="true"/>,
        /// avoiding a redundant round-trip. For unchanged groups only children are applied.
        /// </remarks>
        private async Task ApplyGroupWithChildrenAsync(
            LogGroup sourceGroup,
            LogGroup currentGroup,
            bool groupChanged,
            bool autoDisable,
            ProjectCompareAndApplyResult result,
            CancellationToken cancellationToken)
        {
            bool wasEnabled = autoDisable && (currentGroup.Enabled == true);
            bool? originalSourceEnabled = sourceGroup.Enabled;

            try
            {
                if (groupChanged)
                {
                    // When auto-disabling: fold the disable into the group-property PUT by temporarily
                    // marking Enabled=false on the source before sending the update.
                    if (wasEnabled)
                        sourceGroup.Enabled = false;

                    if (await m_kepwareApiClient.GenericConfig
                            .UpdateItemAsync(sourceGroup, oldItem: null, cancellationToken)
                            .ConfigureAwait(false))
                        result.AddUpdateSuccess();
                    else
                        result.AddFailure(new ApplyFailure
                        {
                            Operation = ApplyOperation.Update,
                            AttemptedItem = sourceGroup,
                        });
                }
                else if (wasEnabled)
                {
                    // Unchanged group but auto-disable requested: explicitly disable before children.
                    sourceGroup.Enabled = false;
                    await m_kepwareApiClient.GenericConfig
                        .UpdateItemAsync(sourceGroup, oldItem: null, cancellationToken)
                        .ConfigureAwait(false);
                }

                // Apply children in mandatory order: log items → column mappings → triggers.
                await ApplyGroupChildrenAsync(sourceGroup, currentGroup, result, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                // Re-enable only if the desired final state is enabled (originalSourceEnabled != false).
                // If source explicitly wants the group disabled, skip the re-enable PUT.
                if (wasEnabled && originalSourceEnabled != false)
                {
                    sourceGroup.Enabled = originalSourceEnabled;
                    await m_kepwareApiClient.GenericConfig
                        .UpdateItemAsync(sourceGroup, oldItem: null, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Applies log items, column mappings, and triggers for a single log group pair in the
        /// required order. The group's enabled/disabled state is assumed to already be correct.
        /// </summary>
        private async Task ApplyGroupChildrenAsync(
            LogGroup sourceGroup,
            LogGroup currentGroup,
            ProjectCompareAndApplyResult result,
            CancellationToken cancellationToken)
        {
            // Step 2: Log items.
            var logItemCompare = await m_kepwareApiClient.GenericConfig
                .CompareAndApplyDetailedAsync<LogItemCollection, LogItem>(
                    sourceGroup.LogItems, currentGroup.LogItems, currentGroup, cancellationToken)
                .ConfigureAwait(false);
            result.Add(logItemCompare);

            // Step 3: Column mappings.
            // ColumnMappings are server-generated; re-fetch if log items were added or removed
            // because the server regenerates the mapping set when the log item set changes.
            ColumnMappingCollection? currentColumnMappings;
            if (logItemCompare.Inserts > 0 || logItemCompare.Deletes > 0)
            {
                currentColumnMappings = await m_kepwareApiClient.GenericConfig
                    .LoadCollectionAsync<ColumnMappingCollection, ColumnMapping>(
                        currentGroup, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                currentColumnMappings = currentGroup.ColumnMappings;
            }

            if (sourceGroup.ColumnMappings != null && currentColumnMappings != null)
            {
                var currentByName = currentColumnMappings
                    .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

                foreach (var sourceMapping in sourceGroup.ColumnMappings)
                {
                    if (currentByName.TryGetValue(sourceMapping.Name, out var currentMapping)
                        && sourceMapping.Hash != currentMapping.Hash)
                    {
                        if (await m_kepwareApiClient.GenericConfig
                                .UpdateItemAsync(sourceMapping, currentMapping, cancellationToken)
                                .ConfigureAwait(false))
                            result.AddUpdateSuccess();
                        else
                            result.AddFailure(new ApplyFailure
                            {
                                Operation = ApplyOperation.Update,
                                AttemptedItem = sourceMapping,
                            });
                    }
                }
            }

            // Step 4: Triggers.
            var triggerCompare = await m_kepwareApiClient.GenericConfig
                .CompareAndApplyDetailedAsync<TriggerCollection, Trigger>(
                    sourceGroup.Triggers, currentGroup.Triggers, currentGroup, cancellationToken)
                .ConfigureAwait(false);
            result.Add(triggerCompare);
        }

        #endregion

        #region Auto-Disable Helper

        /// <summary>
        /// Wraps an operation with optional auto-disable/re-enable of the specified log group.
        /// If <paramref name="autoDisable"/> is true and the group is currently enabled,
        /// the group is disabled before the operation and re-enabled after it completes.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="group">The log group to potentially disable/re-enable.</param>
        /// <param name="autoDisable">When true, applies the auto-disable/re-enable behavior.</param>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        private async Task<T> WithAutoDisableAsync<T>(
            LogGroup group,
            bool autoDisable,
            Func<Task<T>> operation,
            CancellationToken cancellationToken)
        {
            bool wasEnabled = false;
            if (autoDisable && (group.Enabled == true))
            {
                wasEnabled = true;
                group.Enabled = false;
                await m_kepwareApiClient.GenericConfig.UpdateItemAsync(group, oldItem: null, cancellationToken);
            }

            try
            {
                return await operation();
            }
            finally
            {
                if (wasEnabled)
                {
                    group.Enabled = true;
                    await m_kepwareApiClient.GenericConfig.UpdateItemAsync(group, oldItem: null, cancellationToken);
                }
            }
        }

        #endregion
    }
}
