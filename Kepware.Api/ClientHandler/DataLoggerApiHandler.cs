using Kepware.Api.Model;
using Microsoft.Extensions.Logging;

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
        public Task<bool> UpdateLogGroupAsync(LogGroup group, bool autoDisable = false, CancellationToken cancellationToken = default)
            => WithAutoDisableAsync(group, autoDisable,
                () => m_kepwareApiClient.GenericConfig.UpdateItemAsync(group, oldItem: null, cancellationToken),
                cancellationToken);

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
