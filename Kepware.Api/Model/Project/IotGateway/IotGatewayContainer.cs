using System.Text.Json.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Container class representing the <c>_iot_gateway</c> node in the full project JSON.
    /// Holds the three agent collections (MQTT Client, REST Client, REST Server).
    /// </summary>
    public class IotGatewayContainer
    {
        /// <summary>
        /// Gets or sets the MQTT Client agents.
        /// </summary>
        [JsonPropertyName("mqtt_clients")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MqttClientAgentCollection? MqttClientAgents { get; set; }

        /// <summary>
        /// Gets or sets the REST Client agents.
        /// </summary>
        [JsonPropertyName("rest_clients")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RestClientAgentCollection? RestClientAgents { get; set; }

        /// <summary>
        /// Gets or sets the REST Server agents.
        /// </summary>
        [JsonPropertyName("rest_servers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RestServerAgentCollection? RestServerAgents { get; set; }

        /// <summary>
        /// Returns true if all three agent collections are null or empty.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty =>
            (MqttClientAgents == null || MqttClientAgents.Count == 0) &&
            (RestClientAgents == null || RestClientAgents.Count == 0) &&
            (RestServerAgents == null || RestServerAgents.Count == 0);
    }
}
