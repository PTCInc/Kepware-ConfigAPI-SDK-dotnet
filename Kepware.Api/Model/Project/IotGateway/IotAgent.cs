using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Base class for all IoT Gateway agent types. Contains the universal properties
    /// shared by MQTT Client, REST Client, and REST Server agents.
    /// </summary>
    public class IotAgent : NamedEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IotAgent"/> class.
        /// </summary>
        public IotAgent()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotAgent"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        public IotAgent(string name) : base(name)
        {
        }

        #region Properties

        /// <summary>
        /// Gets or sets the IoT Items in this agent.
        /// </summary>
        [YamlIgnore]
        [JsonPropertyName("iot_items")]
        [JsonPropertyOrder(100)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IotItemCollection? IotItems { get; set; }

        /// <summary>
        /// Gets the agent type identifier (read-only).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? AgentType
        {
            get => GetDynamicProperty<string>(Properties.IotAgent.AgentType);
        }

        /// <summary>
        /// Gets or sets whether the agent is enabled.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? Enabled
        {
            get => GetDynamicProperty<bool>(Properties.IotAgent.Enabled);
            set => SetDynamicProperty(Properties.IotAgent.Enabled, value);
        }

        /// <summary>
        /// Gets or sets whether quality changes should be ignored.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? IgnoreQualityChanges
        {
            get => GetDynamicProperty<bool>(Properties.IotAgent.IgnoreQualityChanges);
            set => SetDynamicProperty(Properties.IotAgent.IgnoreQualityChanges, value);
        }

        /// <summary>
        /// Gets the total number of tags configured under this agent (read-only).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ThisAgentTotal
        {
            get => GetDynamicProperty<int>(Properties.IotAgent.ThisAgentTotal);
        }

        /// <summary>
        /// Gets the total number of tags configured under all agents (read-only).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? AllAgentsTotal
        {
            get => GetDynamicProperty<int>(Properties.IotAgent.AllAgentsTotal);
        }

        /// <summary>
        /// Gets the license limit for configured tags (read-only).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? LicenseLimit
        {
            get => GetDynamicProperty<string>(Properties.IotAgent.LicenseLimit);
        }

        #endregion

        /// <summary>
        /// Recursively cleans up the agent and all IoT Items.
        /// </summary>
        /// <param name="defaultValueProvider">The default value provider.</param>
        /// <param name="blnRemoveProjectId">Whether to remove the project ID.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous cleanup operation.</returns>
        public override async Task Cleanup(IKepwareDefaultValueProvider defaultValueProvider, bool blnRemoveProjectId = false, CancellationToken cancellationToken = default)
        {
            await base.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);

            if (IotItems != null)
            {
                foreach (var item in IotItems)
                {
                    await item.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
