using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a REST Client agent in the IoT Gateway.
    /// </summary>
    [Endpoint("/config/v1/project/_iot_gateway/rest_clients/{name}")]
    public class RestClientAgent : PublishingIotAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestClientAgent"/> class.
        /// </summary>
        public RestClientAgent()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestClientAgent"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        public RestClientAgent(string name) : base(name)
        {
        }

        #region REST Client Properties

        /// <summary>
        /// Gets or sets the REST endpoint URL.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Url
        {
            get => GetDynamicProperty<string>(Properties.RestClientAgent.Url);
            set => SetDynamicProperty(Properties.RestClientAgent.Url, value);
        }

        /// <summary>
        /// Gets or sets the HTTP method for publishing data (POST or PUT).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public RestClientHttpMethod? HttpMethod
        {
            get => (RestClientHttpMethod?)GetDynamicProperty<int>(Properties.RestClientAgent.HttpMethod);
            set => SetDynamicProperty(Properties.RestClientAgent.HttpMethod, (int?)value);
        }

        /// <summary>
        /// Gets or sets the HTTP header name-value pairs.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? HttpHeader
        {
            get => GetDynamicProperty<string>(Properties.RestClientAgent.HttpHeader);
            set => SetDynamicProperty(Properties.RestClientAgent.HttpHeader, value);
        }

        /// <summary>
        /// Gets or sets the content-type for published data.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public RestClientMediaType? PublishMediaType
        {
            get => (RestClientMediaType?)GetDynamicProperty<int>(Properties.RestClientAgent.PublishMediaType);
            set => SetDynamicProperty(Properties.RestClientAgent.PublishMediaType, (int?)value);
        }

        /// <summary>
        /// Gets or sets the username for basic HTTP authentication.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Username
        {
            get => GetDynamicProperty<string>(Properties.RestClientAgent.Username);
            set => SetDynamicProperty(Properties.RestClientAgent.Username, value);
        }

        /// <summary>
        /// Gets or sets the password for basic HTTP authentication.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Password
        {
            get => GetDynamicProperty<string>(Properties.RestClientAgent.Password);
            set => SetDynamicProperty(Properties.RestClientAgent.Password, value);
        }

        /// <summary>
        /// Gets or sets whether updates should be buffered when a publish fails.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? BufferOnFailedPublish
        {
            get => GetDynamicProperty<bool>(Properties.RestClientAgent.BufferOnFailedPublish);
            set => SetDynamicProperty(Properties.RestClientAgent.BufferOnFailedPublish, value);
        }

        #endregion
    }
}
