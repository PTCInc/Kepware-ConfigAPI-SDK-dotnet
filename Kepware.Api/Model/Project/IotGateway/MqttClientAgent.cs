using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents an MQTT Client agent in the IoT Gateway.
    /// </summary>
    [Endpoint("/config/v1/project/_iot_gateway/mqtt_clients/{name}")]
    public class MqttClientAgent : PublishingIotAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientAgent"/> class.
        /// </summary>
        public MqttClientAgent()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientAgent"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        public MqttClientAgent(string name) : base(name)
        {
        }

        #region MQTT Properties

        /// <summary>
        /// Gets or sets the MQTT broker URL.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Url
        {
            get => GetDynamicProperty<string>(Properties.MqttClientAgent.Url);
            set => SetDynamicProperty(Properties.MqttClientAgent.Url, value);
        }

        /// <summary>
        /// Gets or sets the MQTT topic for publishing data.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Topic
        {
            get => GetDynamicProperty<string>(Properties.MqttClientAgent.Topic);
            set => SetDynamicProperty(Properties.MqttClientAgent.Topic, value);
        }

        /// <summary>
        /// Gets or sets the MQTT Quality of Service level.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public MqttQos? Qos
        {
            get => (MqttQos?)GetDynamicProperty<int>(Properties.MqttClientAgent.Qos);
            set => SetDynamicProperty(Properties.MqttClientAgent.Qos, (int?)value);
        }

        /// <summary>
        /// Gets or sets the client ID for broker communication.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ClientId
        {
            get => GetDynamicProperty<string>(Properties.MqttClientAgent.ClientId);
            set => SetDynamicProperty(Properties.MqttClientAgent.ClientId, value);
        }

        /// <summary>
        /// Gets or sets the username for broker authentication.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Username
        {
            get => GetDynamicProperty<string>(Properties.MqttClientAgent.Username);
            set => SetDynamicProperty(Properties.MqttClientAgent.Username, value);
        }

        /// <summary>
        /// Gets or sets the password for broker authentication.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Password
        {
            get => GetDynamicProperty<string>(Properties.MqttClientAgent.Password);
            set => SetDynamicProperty(Properties.MqttClientAgent.Password, value);
        }

        /// <summary>
        /// Gets or sets the TLS version for secure connections.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public MqttTlsVersion? TlsVersion
        {
            get => (MqttTlsVersion?)GetDynamicProperty<int>(Properties.MqttClientAgent.TlsVersion);
            set => SetDynamicProperty(Properties.MqttClientAgent.TlsVersion, (int?)value);
        }

        /// <summary>
        /// Gets or sets whether client certificate authentication is enabled.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ClientCertificate
        {
            get => GetDynamicProperty<bool>(Properties.MqttClientAgent.ClientCertificate);
            set => SetDynamicProperty(Properties.MqttClientAgent.ClientCertificate, value);
        }

        #endregion

        #region Last Will Properties

        /// <summary>
        /// Gets or sets whether Last Will and Testament is enabled.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableLastWill
        {
            get => GetDynamicProperty<bool>(Properties.MqttClientAgent.EnableLastWill);
            set => SetDynamicProperty(Properties.MqttClientAgent.EnableLastWill, value);
        }

        /// <summary>
        /// Gets or sets the Last Will and Testament topic.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? LastWillTopic
        {
            get => GetDynamicProperty<string>(Properties.MqttClientAgent.LastWillTopic);
            set => SetDynamicProperty(Properties.MqttClientAgent.LastWillTopic, value);
        }

        /// <summary>
        /// Gets or sets the Last Will and Testament message.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? LastWillMessage
        {
            get => GetDynamicProperty<string>(Properties.MqttClientAgent.LastWillMessage);
            set => SetDynamicProperty(Properties.MqttClientAgent.LastWillMessage, value);
        }

        #endregion

        #region Subscription Properties

        /// <summary>
        /// Gets or sets whether write request listening is enabled.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableWriteTopic
        {
            get => GetDynamicProperty<bool>(Properties.MqttClientAgent.EnableWriteTopic);
            set => SetDynamicProperty(Properties.MqttClientAgent.EnableWriteTopic, value);
        }

        /// <summary>
        /// Gets or sets the topic for write request subscriptions.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? WriteTopic
        {
            get => GetDynamicProperty<string>(Properties.MqttClientAgent.WriteTopic);
            set => SetDynamicProperty(Properties.MqttClientAgent.WriteTopic, value);
        }

        #endregion
    }
}
