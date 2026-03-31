namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents the collection of MQTT Client agents in the IoT Gateway.
    /// </summary>
    [Endpoint("/config/v1/project/_iot_gateway/mqtt_clients")]
    public class MqttClientAgentCollection : EntityCollection<MqttClientAgent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientAgentCollection"/> class.
        /// </summary>
        public MqttClientAgentCollection() { }
    }

    /// <summary>
    /// Represents the collection of REST Client agents in the IoT Gateway.
    /// </summary>
    [Endpoint("/config/v1/project/_iot_gateway/rest_clients")]
    public class RestClientAgentCollection : EntityCollection<RestClientAgent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestClientAgentCollection"/> class.
        /// </summary>
        public RestClientAgentCollection() { }
    }

    /// <summary>
    /// Represents the collection of REST Server agents in the IoT Gateway.
    /// </summary>
    [Endpoint("/config/v1/project/_iot_gateway/rest_servers")]
    public class RestServerAgentCollection : EntityCollection<RestServerAgent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestServerAgentCollection"/> class.
        /// </summary>
        public RestServerAgentCollection() { }
    }

    /// <summary>
    /// Represents the collection of IoT Items in an IoT Gateway agent.
    /// </summary>
    [Endpoint("/config/v1/project/_iot_gateway/mqtt_clients/{agentName}/iot_items")]
    public class IotItemCollection : EntityCollection<IotItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IotItemCollection"/> class.
        /// </summary>
        public IotItemCollection() { }
    }
}
