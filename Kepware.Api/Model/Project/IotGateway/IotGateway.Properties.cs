namespace Kepware.Api.Model
{
    public partial class Properties
    {
        /// <summary>
        /// Property key constants shared by all IoT Gateway agent types.
        /// </summary>
        public static class IotAgent
        {
            /// <summary>
            /// The agent type identifier (read-only).
            /// </summary>
            public const string AgentType = "iot_gateway.AGENTTYPES_TYPE";

            /// <summary>
            /// Specifies if the agent is enabled.
            /// </summary>
            public const string Enabled = "iot_gateway.AGENTTYPES_ENABLED";

            /// <summary>
            /// Specifies if changes in quality should be ignored.
            /// </summary>
            public const string IgnoreQualityChanges = "iot_gateway.IGNORE_QUALITY_CHANGES";

            /// <summary>
            /// Specifies the publish type (Interval or On Data Change).
            /// </summary>
            public const string PublishType = "iot_gateway.AGENTTYPES_PUBLISH_TYPE";

            /// <summary>
            /// Specifies the publish rate in milliseconds.
            /// </summary>
            public const string RateMs = "iot_gateway.AGENTTYPES_RATE_MS";

            /// <summary>
            /// Specifies the publish format (Wide or Narrow).
            /// </summary>
            public const string PublishFormat = "iot_gateway.AGENTTYPES_PUBLISH_FORMAT";

            /// <summary>
            /// Maximum number of tag events per publish in narrow format.
            /// </summary>
            public const string MaxEventsPerPublish = "iot_gateway.AGENTTYPES_MAX_EVENTS";

            /// <summary>
            /// Transaction timeout in seconds.
            /// </summary>
            public const string TransactionTimeoutS = "iot_gateway.AGENTTYPES_TIMEOUT_S";

            /// <summary>
            /// Specifies the message format (Standard or Advanced Template).
            /// </summary>
            public const string MessageFormat = "iot_gateway.AGENTTYPES_MESSAGE_FORMAT";

            /// <summary>
            /// Standard template for message formatting.
            /// </summary>
            public const string StandardTemplate = "iot_gateway.AGENTTYPES_STANDARD_TEMPLATE";

            /// <summary>
            /// Expansion of |VALUES| in standard template.
            /// </summary>
            public const string ExpansionOfValues = "iot_gateway.AGENTTYPES_EXPANSION_OF_VALUES";

            /// <summary>
            /// Advanced template for message formatting.
            /// </summary>
            public const string AdvancedTemplate = "iot_gateway.AGENTTYPES_ADVANCED_TEMPLATE";

            /// <summary>
            /// Specifies if the initial update should be sent when the agent starts.
            /// </summary>
            public const string SendInitialUpdate = "iot_gateway.AGENTTYPES_SEND_INITIAL_UPDATE";

            /// <summary>
            /// Total number of tags configured under this agent (read-only).
            /// </summary>
            public const string ThisAgentTotal = Properties.NonSerialized.ThisAgentTotal;

            /// <summary>
            /// Total number of tags configured under all agents (read-only).
            /// </summary>
            public const string AllAgentsTotal = Properties.NonSerialized.AllAgentsTotal;

            /// <summary>
            /// Maximum number of configured tags allowed by the license (read-only).
            /// </summary>
            public const string LicenseLimit = Properties.NonSerialized.LicenseLimit;
        }

        /// <summary>
        /// Property key constants specific to MQTT Client agents.
        /// </summary>
        public static class MqttClientAgent
        {
            /// <summary>
            /// URL of the MQTT broker endpoint.
            /// </summary>
            public const string Url = "iot_gateway.MQTT_CLIENT_URL";

            /// <summary>
            /// Topic name for publishing data on the broker.
            /// </summary>
            public const string Topic = "iot_gateway.MQTT_CLIENT_TOPIC";

            /// <summary>
            /// MQTT Quality of Service level.
            /// </summary>
            public const string Qos = "iot_gateway.MQTT_CLIENT_QOS";

            /// <summary>
            /// Unique client identity for broker communication.
            /// </summary>
            public const string ClientId = "iot_gateway.MQTT_CLIENT_CLIENT_ID";

            /// <summary>
            /// Username for broker authentication.
            /// </summary>
            public const string Username = "iot_gateway.MQTT_CLIENT_USERNAME";

            /// <summary>
            /// Password for broker authentication.
            /// </summary>
            public const string Password = "iot_gateway.MQTT_CLIENT_PASSWORD";

            /// <summary>
            /// TLS version for secure connections.
            /// </summary>
            public const string TlsVersion = "iot_gateway.MQTT_TLS_VERSION";

            /// <summary>
            /// Enable client certificate for application-based authentication.
            /// </summary>
            public const string ClientCertificate = "iot_gateway.MQTT_CLIENT_CERTIFICATE";

            /// <summary>
            /// Enable Last Will and Testament.
            /// </summary>
            public const string EnableLastWill = "iot_gateway.MQTT_CLIENT_ENABLE_LAST_WILL";

            /// <summary>
            /// Topic for Last Will and Testament message.
            /// </summary>
            public const string LastWillTopic = "iot_gateway.MQTT_CLIENT_LAST_WILL_TOPIC";

            /// <summary>
            /// Last Will and Testament message text.
            /// </summary>
            public const string LastWillMessage = "iot_gateway.MQTT_CLIENT_LAST_WILL_MESSAGE";

            /// <summary>
            /// Enable listening for write requests.
            /// </summary>
            public const string EnableWriteTopic = "iot_gateway.MQTT_CLIENT_ENABLE_WRITE_TOPIC";

            /// <summary>
            /// Topic for write request subscriptions.
            /// </summary>
            public const string WriteTopic = "iot_gateway.MQTT_CLIENT_WRITE_TOPIC";
        }

        /// <summary>
        /// Property key constants specific to REST Client agents.
        /// </summary>
        public static class RestClientAgent
        {
            /// <summary>
            /// URL of the REST endpoint.
            /// </summary>
            public const string Url = "iot_gateway.REST_CLIENT_URL";

            /// <summary>
            /// HTTP method for publishing data (POST or PUT).
            /// </summary>
            public const string HttpMethod = "iot_gateway.REST_CLIENT_METHOD";

            /// <summary>
            /// HTTP header name-value pairs sent with each connection.
            /// </summary>
            public const string HttpHeader = "iot_gateway.REST_CLIENT_HTTP_HEADER";

            /// <summary>
            /// Content-type header for published data.
            /// </summary>
            public const string PublishMediaType = "iot_gateway.REST_CLIENT_PUBLISH_MEDIA_TYPE";

            /// <summary>
            /// Username for basic HTTP authentication.
            /// </summary>
            public const string Username = "iot_gateway.REST_CLIENT_USERNAME";

            /// <summary>
            /// Password for basic HTTP authentication.
            /// </summary>
            public const string Password = "iot_gateway.REST_CLIENT_PASSWORD";

            /// <summary>
            /// Buffer updates when a publish fails.
            /// </summary>
            public const string BufferOnFailedPublish = "iot_gateway.BUFFER_ON_FAILED_PUBLISH";
        }

        /// <summary>
        /// Property key constants specific to REST Server agents.
        /// </summary>
        public static class RestServerAgent
        {
            /// <summary>
            /// Network adapter for the REST server endpoint.
            /// </summary>
            public const string NetworkAdapter = "iot_gateway.REST_SERVER_NETWORK_ADAPTER";

            /// <summary>
            /// Port number for the REST server.
            /// </summary>
            public const string PortNumber = "iot_gateway.REST_SERVER_PORT_NUMBER";

            /// <summary>
            /// CORS allowed origins (comma-delimited).
            /// </summary>
            public const string CorsAllowedOrigins = "iot_gateway.REST_SERVER_CORS_ALLOWED_ORIGINS";

            /// <summary>
            /// Enable HTTPS encryption.
            /// </summary>
            public const string UseHttps = "iot_gateway.REST_SERVER_USE_HTTPS";

            /// <summary>
            /// Enable write endpoint for tag writes.
            /// </summary>
            public const string EnableWriteEndpoint = "iot_gateway.REST_SERVER_ENABLE_WRITE_ENDPOINT";

            /// <summary>
            /// Allow unauthenticated access.
            /// </summary>
            public const string AllowAnonymousLogin = "iot_gateway.REST_SERVER_ALLOW_ANONYMOUS_LOGIN";
        }

        /// <summary>
        /// Property key constants for IoT Items.
        /// </summary>
        public static class IotItem
        {
            /// <summary>
            /// Full channel.device.name of the referenced server tag.
            /// </summary>
            public const string ServerTag = "iot_gateway.IOT_ITEM_SERVER_TAG";

            /// <summary>
            /// Use scan rate to collect data from the device.
            /// </summary>
            public const string UseScanRate = "iot_gateway.IOT_ITEM_USE_SCAN_RATE";

            /// <summary>
            /// Scan rate in milliseconds.
            /// </summary>
            public const string ScanRateMs = "iot_gateway.IOT_ITEM_SCAN_RATE_MS";

            /// <summary>
            /// Force publish on every scan regardless of value change.
            /// </summary>
            public const string PublishEveryScan = "iot_gateway.IOT_ITEM_SEND_EVERY_SCAN";

            /// <summary>
            /// Deadband percentage for publish threshold.
            /// </summary>
            public const string DeadbandPercent = "iot_gateway.IOT_ITEM_DEADBAND_PERCENT";

            /// <summary>
            /// Enable or disable the IoT Item.
            /// </summary>
            public const string Enabled = "iot_gateway.IOT_ITEM_ENABLED";

            /// <summary>
            /// Data type of the referenced tag.
            /// </summary>
            public const string DataType = "iot_gateway.IOT_ITEM_DATA_TYPE";
        }
    }
}
