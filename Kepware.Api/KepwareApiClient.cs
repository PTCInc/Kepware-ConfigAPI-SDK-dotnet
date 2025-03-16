using Kepware.Api.ClientHandler;
using Kepware.Api.Model;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Kepware.Api
{
    /// <summary>
    /// Client for interacting with the Kepware server.
    /// </summary>
    public partial class KepwareApiClient : IKepwareDefaultValueProvider
    {
        /// <summary>
        /// The value for an unknown client or host name.
        /// </summary>
        public const string UNKNOWN = "Unknown";
        private const string ENDPOINT_STATUS = "/config/v1/status";
        private const string ENDPOINT_ABOUT = "/config/v1/about";

        private readonly ILogger<KepwareApiClient> m_logger;
        private readonly HttpClient m_httpClient;


        private bool? m_blnIsConnected = null;

        /// <summary>
        /// Gets the name of the client.
        /// </summary>
        public string ClientName { get; }

        /// <summary>
        /// Gets the host name of the client.
        /// </summary>
        public string ClientHostName => m_httpClient.BaseAddress?.Host ?? UNKNOWN;

        public KepwareApiClientOptions ClientOptions { get; init; }

        public GenericApiHandler GenericConfig { get; init; }
        public ProjectApiHandler Project { get; init; }

        public AdminApiHandler Admin { get; init; }

        public ServicesApiHandler ApiServices { get; init; }

        internal HttpClient HttpClient { get { return m_httpClient; } }

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="KepwareApiClient"/> class.
        /// </summary>
        /// <param name="options">The client options.</param>
        /// <param name="loggerFactory">The loggerFactory instance.</param>
        /// <param name="httpClient">The HTTP client instance.</param>
        public KepwareApiClient(KepwareApiClientOptions options, ILoggerFactory loggerFactory, HttpClient httpClient)
            : this(UNKNOWN, options, loggerFactory, httpClient)
        {
        }

        internal KepwareApiClient(string name, KepwareApiClientOptions options, ILoggerFactory loggerFactory, HttpClient httpClient)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(httpClient);

            m_logger = loggerFactory.CreateLogger<KepwareApiClient>();
            m_httpClient = httpClient;
            ClientName = name;
            ClientOptions = options;

            GenericConfig = new GenericApiHandler(this, loggerFactory.CreateLogger<GenericApiHandler>());

            var channelsApiHandler = new ChannelApiHandler(this, loggerFactory.CreateLogger<ChannelApiHandler>());
            var devicesApiHandler = new DeviceApiHandler(this, loggerFactory.CreateLogger<DeviceApiHandler>());
            Project = new ProjectApiHandler(this, channelsApiHandler, devicesApiHandler, loggerFactory.CreateLogger<ProjectApiHandler>());
            Admin = new AdminApiHandler(this, loggerFactory.CreateLogger<AdminApiHandler>());
            ApiServices = new ServicesApiHandler(this, loggerFactory.CreateLogger<ServicesApiHandler>());
        }
        #endregion

        #region connection test & product info
        /// <summary>
        /// Tests the connection to the Kepware server.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the connection was successful.</returns>

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            bool blnIsConnected = false;
            try
            {
                if (m_blnIsConnected == null) // first time after connection change
                {
                    m_logger.LogInformation("Connecting to {ClientName}-client at {BaseAddress}...", ClientName, m_httpClient.BaseAddress);
                }
                var response = await m_httpClient.GetAsync(ENDPOINT_STATUS, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var status = await JsonSerializer.DeserializeAsync(
                        await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                        KepJsonContext.Default.ListApiStatus, cancellationToken)
                        .ConfigureAwait(false);
                    if (status?.FirstOrDefault()?.Healthy == true)
                    {
                        blnIsConnected = true;
                    }
                }

                if (m_blnIsConnected == null || (m_blnIsConnected != null && m_blnIsConnected != blnIsConnected)) // first time after connection change or when connection is lost
                {
                    if (!blnIsConnected)
                    {
                        m_logger.LogWarning("Failed to connect to {ClientName}-client at {BaseAddress}, Reason: {ReasonPhrase}", ClientName, m_httpClient.BaseAddress, response.ReasonPhrase);
                    }
                    else
                    {
                        var prodInfo = await GetProductInfoAsync(cancellationToken).ConfigureAwait(false);
                        m_logger.LogInformation("Successfully connected to {ClientName}-client: {ProductName} {ProductVersion} on {BaseAddress}", ClientName, prodInfo?.ProductName, prodInfo?.ProductVersion, m_httpClient.BaseAddress);
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                if (m_blnIsConnected == null || m_blnIsConnected == true) // first time after connection change or when connection is lost
                    m_logger.LogWarning(httpEx, "Failed to connect to {ClientName}-client at {BaseAddress}", ClientName, m_httpClient.BaseAddress);
            }
            m_blnIsConnected = blnIsConnected;
            return blnIsConnected;
        }

        /// <summary>
        /// Gets the product information from the Kepware server.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the product information.</returns>
        public async Task<ProductInfo?> GetProductInfoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await m_httpClient.GetAsync(ENDPOINT_ABOUT, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var prodInfo = JsonSerializer.Deserialize(content, KepJsonContext.Default.ProductInfo);

                    m_blnIsConnected = true;
                    return prodInfo;
                }
                else
                {
                    m_logger.LogWarning("Failed to get product info from endpoint {Endpoint}, Reason: {ReasonPhrase}", "/config/v1/about", response.ReasonPhrase);
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {BaseAddress}", m_httpClient.BaseAddress);
                m_blnIsConnected = null;
            }
            catch (JsonException jsonEx)
            {
                m_logger.LogWarning(jsonEx, "Failed to parse ProductInfo from {BaseAddress}", m_httpClient.BaseAddress);
            }

            return null;
        }
        #endregion

        #region IKepwareDefaultValueProvider
        private readonly ConcurrentDictionary<string, ReadOnlyDictionary<string, JsonElement>> m_driverDefaultValues = [];
        async Task<ReadOnlyDictionary<string, JsonElement>> IKepwareDefaultValueProvider.GetDefaultValuesAsync(string driverName, string entityName, CancellationToken cancellationToken)
        {
            var key = $"{driverName}/{entityName}";
            if (m_driverDefaultValues.TryGetValue(key, out var deviceDefaults))
            {
                return deviceDefaults;
            }
            else
            {
                Docs.CollectionDefinition collectionDefinition = entityName switch
                {
                    nameof(Channel) => await GenericConfig.GetChannelPropertiesAsync(driverName, cancellationToken),
                    nameof(Device) => await GenericConfig.GetDevicePropertiesAsync(driverName, cancellationToken),
                    _ => Docs.CollectionDefinition.Empty,
                };

                var defaults = collectionDefinition?.PropertyDefinitions?
                    .Where(p => !string.IsNullOrEmpty(p.SymbolicName) && p.SymbolicName != Properties.Channel.DeviceDriver)
                    .ToDictionary(p => p.SymbolicName!, p => p.GetDefaultValue()) ?? [];

                return m_driverDefaultValues[key] = new ReadOnlyDictionary<string, JsonElement>(defaults);
            }
        }
        #endregion

        #region internal
        /// <summary>
        /// Invoked by Handler, when they receice a http request exception
        /// </summary>
        /// <param name="httpEx"></param>
        internal void OnHttpRequestException(HttpRequestException httpEx)
        {
            m_blnIsConnected = null;
        }
        #endregion
    }
}
