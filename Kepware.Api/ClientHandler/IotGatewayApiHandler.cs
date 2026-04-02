using Kepware.Api.Model;
using Microsoft.Extensions.Logging;

namespace Kepware.Api.ClientHandler
{
    /// <summary>
    /// Handles operations related to IoT Gateway agent configurations in the Kepware server.
    /// Supports MQTT Client, REST Client, and REST Server agent types and their child IoT Items.
    /// </summary>
    public class IotGatewayApiHandler
    {
        private readonly KepwareApiClient m_kepwareApiClient;
        private readonly ILogger<IotGatewayApiHandler> m_logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IotGatewayApiHandler"/> class.
        /// </summary>
        /// <param name="kepwareApiClient">The Kepware Configuration API client.</param>
        /// <param name="logger">The logger instance.</param>
        public IotGatewayApiHandler(KepwareApiClient kepwareApiClient, ILogger<IotGatewayApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_logger = logger;
        }

        #region MQTT Client Agent

        /// <summary>
        /// Gets or creates an MQTT Client agent with the specified name.
        /// If the agent exists, it is loaded and returned. If it does not exist, it is created with the specified properties.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        /// <param name="properties">Optional properties to set on the agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created or loaded <see cref="MqttClientAgent"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the agent cannot be created or loaded.</exception>
        public async Task<MqttClientAgent> GetOrCreateMqttClientAgentAsync(string name, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));

            var agent = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<MqttClientAgent>(name, cancellationToken: cancellationToken);

            if (agent == null)
            {
                agent = await CreateMqttClientAgentAsync(name, properties, cancellationToken);
                if (agent == null)
                {
                    throw new InvalidOperationException($"Failed to create or load MQTT Client agent '{name}'");
                }
            }

            return agent;
        }

        /// <summary>
        /// Gets an MQTT Client agent with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="MqttClientAgent"/> or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task<MqttClientAgent?> GetMqttClientAgentAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));

            return await m_kepwareApiClient.GenericConfig.LoadEntityAsync<MqttClientAgent>(name, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Creates a new MQTT Client agent with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        /// <param name="properties">Optional properties to set on the agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created <see cref="MqttClientAgent"/>, or null if creation failed.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task<MqttClientAgent?> CreateMqttClientAgentAsync(string name, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));

            var agent = new MqttClientAgent(name);
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    agent.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<MqttClientAgentCollection, MqttClientAgent>(agent, cancellationToken: cancellationToken))
            {
                return agent;
            }

            return null;
        }

        /// <summary>
        /// Updates the specified MQTT Client agent.
        /// </summary>
        /// <param name="agent">The agent to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the update was successful.</returns>
        public Task<bool> UpdateMqttClientAgentAsync(MqttClientAgent agent, CancellationToken cancellationToken = default)
            => m_kepwareApiClient.GenericConfig.UpdateItemAsync(agent, oldItem: null, cancellationToken);

        /// <summary>
        /// Deletes the specified MQTT Client agent.
        /// </summary>
        /// <param name="agent">The agent to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the agent is null.</exception>
        public Task<bool> DeleteMqttClientAgentAsync(MqttClientAgent agent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(agent);
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync(agent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes the MQTT Client agent with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public Task<bool> DeleteMqttClientAgentAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync<MqttClientAgent>(name, cancellationToken: cancellationToken);
        }

        #endregion

        #region REST Client Agent

        /// <summary>
        /// Gets or creates a REST Client agent with the specified name.
        /// If the agent exists, it is loaded and returned. If it does not exist, it is created with the specified properties.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        /// <param name="properties">Optional properties to set on the agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created or loaded <see cref="RestClientAgent"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the agent cannot be created or loaded.</exception>
        public async Task<RestClientAgent> GetOrCreateRestClientAgentAsync(string name, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));

            var agent = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<RestClientAgent>(name, cancellationToken: cancellationToken);

            if (agent == null)
            {
                agent = await CreateRestClientAgentAsync(name, properties, cancellationToken);
                if (agent == null)
                {
                    throw new InvalidOperationException($"Failed to create or load REST Client agent '{name}'");
                }
            }

            return agent;
        }

        /// <summary>
        /// Gets a REST Client agent with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="RestClientAgent"/> or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task<RestClientAgent?> GetRestClientAgentAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));

            return await m_kepwareApiClient.GenericConfig.LoadEntityAsync<RestClientAgent>(name, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Creates a new REST Client agent with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        /// <param name="properties">Optional properties to set on the agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created <see cref="RestClientAgent"/>, or null if creation failed.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task<RestClientAgent?> CreateRestClientAgentAsync(string name, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));

            var agent = new RestClientAgent(name);
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    agent.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<RestClientAgentCollection, RestClientAgent>(agent, cancellationToken: cancellationToken))
            {
                return agent;
            }

            return null;
        }

        /// <summary>
        /// Updates the specified REST Client agent.
        /// </summary>
        /// <param name="agent">The agent to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the update was successful.</returns>
        public Task<bool> UpdateRestClientAgentAsync(RestClientAgent agent, CancellationToken cancellationToken = default)
            => m_kepwareApiClient.GenericConfig.UpdateItemAsync(agent, oldItem: null, cancellationToken);

        /// <summary>
        /// Deletes the specified REST Client agent.
        /// </summary>
        /// <param name="agent">The agent to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the agent is null.</exception>
        public Task<bool> DeleteRestClientAgentAsync(RestClientAgent agent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(agent);
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync(agent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes the REST Client agent with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public Task<bool> DeleteRestClientAgentAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync<RestClientAgent>(name, cancellationToken: cancellationToken);
        }

        #endregion

        #region REST Server Agent

        /// <summary>
        /// Gets or creates a REST Server agent with the specified name.
        /// If the agent exists, it is loaded and returned. If it does not exist, it is created with the specified properties.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        /// <param name="properties">Optional properties to set on the agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created or loaded <see cref="RestServerAgent"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the agent cannot be created or loaded.</exception>
        public async Task<RestServerAgent> GetOrCreateRestServerAgentAsync(string name, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));

            var agent = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<RestServerAgent>(name, cancellationToken: cancellationToken);

            if (agent == null)
            {
                agent = await CreateRestServerAgentAsync(name, properties, cancellationToken);
                if (agent == null)
                {
                    throw new InvalidOperationException($"Failed to create or load REST Server agent '{name}'");
                }
            }

            return agent;
        }

        /// <summary>
        /// Gets a REST Server agent with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="RestServerAgent"/> or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task<RestServerAgent?> GetRestServerAgentAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));

            return await m_kepwareApiClient.GenericConfig.LoadEntityAsync<RestServerAgent>(name, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Creates a new REST Server agent with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        /// <param name="properties">Optional properties to set on the agent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created <see cref="RestServerAgent"/>, or null if creation failed.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task<RestServerAgent?> CreateRestServerAgentAsync(string name, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));

            var agent = new RestServerAgent(name);
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    agent.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<RestServerAgentCollection, RestServerAgent>(agent, cancellationToken: cancellationToken))
            {
                return agent;
            }

            return null;
        }

        /// <summary>
        /// Updates the specified REST Server agent.
        /// </summary>
        /// <param name="agent">The agent to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the update was successful.</returns>
        public Task<bool> UpdateRestServerAgentAsync(RestServerAgent agent, CancellationToken cancellationToken = default)
            => m_kepwareApiClient.GenericConfig.UpdateItemAsync(agent, oldItem: null, cancellationToken);

        /// <summary>
        /// Deletes the specified REST Server agent.
        /// </summary>
        /// <param name="agent">The agent to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the agent is null.</exception>
        public Task<bool> DeleteRestServerAgentAsync(RestServerAgent agent, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(agent);
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync(agent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes the REST Server agent with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public Task<bool> DeleteRestServerAgentAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync<RestServerAgent>(name, cancellationToken: cancellationToken);
        }

        #endregion

        #region IoT Items

        /// <summary>
        /// Converts a dot-delimited server tag name to the IoT Item name used by the Kepware API.
        /// Replaces dots with underscores and strips a leading underscore if present.
        /// For example, "Channel1.Device1.Tag1" becomes "Channel1_Device1_Tag1"
        /// and "_System._Time" becomes "System__Time".
        /// </summary>
        /// <param name="serverTag">The dot-delimited server tag name.</param>
        /// <returns>The converted IoT Item name.</returns>
        internal static string ServerTagToItemName(string serverTag)
        {
            var name = serverTag.Replace('.', '_');
            if (name.StartsWith('_'))
                name = name[1..];
            return name;
        }

        /// <summary>
        /// Gets or creates an IoT Item for the specified server tag under the given parent agent.
        /// If the item exists, it is loaded and returned. If it does not exist, it is created with the specified properties.
        /// The IoT Item name is derived from the server tag by replacing dots with underscores and stripping any leading underscore.
        /// </summary>
        /// <param name="serverTag">The dot-delimited server tag reference (e.g., "Channel1.Device1.Tag1").</param>
        /// <param name="parentAgent">The parent agent that will own the IoT Item.</param>
        /// <param name="properties">Optional properties to set on the IoT Item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created or loaded <see cref="IotItem"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the server tag is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent agent is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the item cannot be created or loaded.</exception>
        public async Task<IotItem> GetOrCreateIotItemAsync(string serverTag, IotAgent parentAgent, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serverTag))
                throw new ArgumentException("Server tag cannot be null or empty", nameof(serverTag));
            ArgumentNullException.ThrowIfNull(parentAgent);

            var itemName = ServerTagToItemName(serverTag);
            var item = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<IotItem>(itemName, parentAgent, cancellationToken: cancellationToken);

            if (item == null)
            {
                item = await CreateIotItemAsync(serverTag, parentAgent, properties, cancellationToken);
                if (item == null)
                {
                    throw new InvalidOperationException($"Failed to create or load IoT Item for server tag '{serverTag}'");
                }
            }

            return item;
        }

        /// <summary>
        /// Gets an IoT Item by its server tag name under the given parent agent.
        /// The server tag is converted to the IoT Item name by replacing dots with underscores and stripping any leading underscore.
        /// </summary>
        /// <param name="serverTag">The dot-delimited server tag name (e.g., "Channel1.Device1.Tag1").</param>
        /// <param name="parentAgent">The parent agent that owns the IoT Item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The loaded <see cref="IotItem"/> or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when the server tag is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent agent is null.</exception>
        public async Task<IotItem?> GetIotItemAsync(string serverTag, IotAgent parentAgent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serverTag))
                throw new ArgumentException("Server tag cannot be null or empty", nameof(serverTag));
            ArgumentNullException.ThrowIfNull(parentAgent);

            var itemName = ServerTagToItemName(serverTag);
            return await m_kepwareApiClient.GenericConfig.LoadEntityAsync<IotItem>(itemName, parentAgent, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Creates a new IoT Item for the specified server tag under the given parent agent.
        /// The IoT Item name is derived from the server tag by replacing dots with underscores and stripping any leading underscore.
        /// </summary>
        /// <param name="serverTag">The dot-delimited server tag reference (e.g., "Channel1.Device1.Tag1").</param>
        /// <param name="parentAgent">The parent agent that will own the IoT Item.</param>
        /// <param name="properties">Optional properties to set on the IoT Item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created <see cref="IotItem"/>, or null if creation failed.</returns>
        /// <exception cref="ArgumentException">Thrown when the server tag is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent agent is null.</exception>
        public async Task<IotItem?> CreateIotItemAsync(string serverTag, IotAgent parentAgent, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serverTag))
                throw new ArgumentException("Server tag cannot be null or empty", nameof(serverTag));
            ArgumentNullException.ThrowIfNull(parentAgent);

            var itemName = ServerTagToItemName(serverTag);
            var item = new IotItem(itemName) { Owner = parentAgent };
            item.SetDynamicProperty(Properties.IotItem.ServerTag, serverTag);

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    item.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<IotItemCollection, IotItem>(item, parentAgent, cancellationToken: cancellationToken))
            {
                return item;
            }

            return null;
        }

        /// <summary>
        /// Updates the specified IoT Item.
        /// </summary>
        /// <param name="item">The IoT Item to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the update was successful.</returns>
        public Task<bool> UpdateIotItemAsync(IotItem item, CancellationToken cancellationToken = default)
            => m_kepwareApiClient.GenericConfig.UpdateItemAsync(item, oldItem: null, cancellationToken);

        /// <summary>
        /// Deletes the specified IoT Item.
        /// </summary>
        /// <param name="item">The IoT Item to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the item is null.</exception>
        public Task<bool> DeleteIotItemAsync(IotItem item, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync(item, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes the IoT Item identified by the specified server tag under the given parent agent.
        /// The server tag is converted to the IoT Item name by replacing dots with underscores and stripping any leading underscore.
        /// </summary>
        /// <param name="serverTag">The dot-delimited server tag name (e.g., "Channel1.Device1.Tag1").</param>
        /// <param name="parentAgent">The parent agent that owns the IoT Item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the server tag is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the parent agent is null.</exception>
        public Task<bool> DeleteIotItemAsync(string serverTag, IotAgent parentAgent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(serverTag))
                throw new ArgumentException("Server tag cannot be null or empty", nameof(serverTag));
            ArgumentNullException.ThrowIfNull(parentAgent);
            var itemName = ServerTagToItemName(serverTag);
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync<IotItem>([parentAgent.Name!, itemName], cancellationToken: cancellationToken);
        }

        #endregion
    }
}
