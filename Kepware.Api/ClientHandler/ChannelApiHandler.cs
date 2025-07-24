using Kepware.Api.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.ClientHandler
{
    /// <summary>
    /// Handles operations related to channel configurations in the Kepware server.
    /// </summary>
    public class ChannelApiHandler
    {
        private readonly KepwareApiClient m_kepwareApiClient;
        private readonly ILogger<ChannelApiHandler> m_logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelApiHandler"/> class.
        /// </summary>
        /// <param name="kepwareApiClient">The Kepware Configuration API client.</param>
        /// <param name="logger">The logger instance.</param>
        public ChannelApiHandler(KepwareApiClient kepwareApiClient, ILogger<ChannelApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_logger = logger;
        }

        #region GetOrCreateChannelAsync
        /// <summary>
        /// Gets or creates a channel with the specified name and driver.
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        /// <param name="driverName">The name of the driver.</param>
        /// <param name="properties">The properties to set on the channel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created or loaded <see cref="Channel"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name or driver name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the channel cannot be created or loaded.</exception>
        public async Task<Channel> GetOrCreateChannelAsync(string name, string driverName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(driverName))
                throw new ArgumentException("Driver name cannot be null or empty", nameof(driverName));

            var channel = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>(name, cancellationToken: cancellationToken);

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
                            await m_kepwareApiClient.GenericConfig.UpdateItemAsync(channel, cancellationToken: cancellationToken);
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
        #endregion

        #region GetChannelAsync
        /// <summary>
        /// Gets  a channel with the specified name and driver.
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="Channel"/> or null if it does not exist.</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name or driver name is null or empty.</exception>
        public async Task<Channel?> GetChannelAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(name));

            return await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Channel>(name, cancellationToken: cancellationToken);

        }
        #endregion

        #region CreateChannelAsync

        /// <summary>
        /// Creates a new channel with the specified name and driver.
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        /// <param name="driverName">The name of the driver.</param>
        /// <param name="properties">The properties to set on the channel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="Channel"/>, or null if creation failed.</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name or driver name is null or empty.</exception>
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

            if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<ChannelCollection, Channel>(channel, cancellationToken: cancellationToken))
            {
                return channel;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region UpdateChannelAsync
        /// <summary>
        /// Updates the specified channel.
        /// </summary>
        /// <param name="channel">The channel to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the update was successful.</returns>
        public Task<bool> UpdateChannelAsync(Channel channel, CancellationToken cancellationToken = default)
            => m_kepwareApiClient.GenericConfig.UpdateItemAsync(channel, oldItem: null, cancellationToken);
        #endregion

        #region DeleteChannelAsync
        /// <summary>
        /// Deletes the specified channel.
        /// </summary>
        /// <param name="channel">The channel to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the channel is null.</exception>
        public Task<bool> DeleteChannelAsync(Channel channel, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync(channel, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes the channel with the specified name.
        /// </summary>
        /// <param name="channelName">The name of the channel to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name is null or empty.</exception>
        public Task<bool> DeleteChannelAsync(string channelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(channelName))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(channelName));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync<Channel>(channelName, cancellationToken: cancellationToken);
        }
        #endregion
    }
}
