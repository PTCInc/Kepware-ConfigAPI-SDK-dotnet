using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a REST Server agent in the IoT Gateway.
    /// REST Server agents do not have publish or message template properties.
    /// </summary>
    [Endpoint("/config/v1/project/_iot_gateway/rest_servers/{name}")]
    public class RestServerAgent : IotAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestServerAgent"/> class.
        /// </summary>
        public RestServerAgent()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestServerAgent"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        public RestServerAgent(string name) : base(name)
        {
        }

        #region REST Server Properties

        /// <summary>
        /// Gets or sets the network adapter for the REST server endpoint.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? NetworkAdapter
        {
            get => GetDynamicProperty<string>(Properties.RestServerAgent.NetworkAdapter);
            set => SetDynamicProperty(Properties.RestServerAgent.NetworkAdapter, value);
        }

        /// <summary>
        /// Gets or sets the port number for the REST server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? PortNumber
        {
            get => GetDynamicProperty<int>(Properties.RestServerAgent.PortNumber);
            set => SetDynamicProperty(Properties.RestServerAgent.PortNumber, value);
        }

        /// <summary>
        /// Gets or sets the CORS allowed origins (comma-delimited list).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? CorsAllowedOrigins
        {
            get => GetDynamicProperty<string>(Properties.RestServerAgent.CorsAllowedOrigins);
            set => SetDynamicProperty(Properties.RestServerAgent.CorsAllowedOrigins, value);
        }

        /// <summary>
        /// Gets or sets whether HTTPS encryption is enabled.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? UseHttps
        {
            get => GetDynamicProperty<bool>(Properties.RestServerAgent.UseHttps);
            set => SetDynamicProperty(Properties.RestServerAgent.UseHttps, value);
        }

        /// <summary>
        /// Gets or sets whether the write endpoint is enabled.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableWriteEndpoint
        {
            get => GetDynamicProperty<bool>(Properties.RestServerAgent.EnableWriteEndpoint);
            set => SetDynamicProperty(Properties.RestServerAgent.EnableWriteEndpoint, value);
        }

        /// <summary>
        /// Gets or sets whether anonymous login is allowed.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? AllowAnonymousLogin
        {
            get => GetDynamicProperty<bool>(Properties.RestServerAgent.AllowAnonymousLogin);
            set => SetDynamicProperty(Properties.RestServerAgent.AllowAnonymousLogin, value);
        }

        #endregion
    }
}
