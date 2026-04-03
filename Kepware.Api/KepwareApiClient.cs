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
    /// Client for interacting with the Kepware server Configuration API. Using the <see cref="KepwareApiClient"/> class
    /// provides the ability to create, read, update and delete configuration of a Kepware server instance.
    /// 
    /// All handlers are defined in the <see cref="Kepware.Api.ClientHandler"/> namespace.
    /// </summary>
    public partial class KepwareApiClient : IKepwareDefaultValueProvider
    {
        /// <summary>
        /// The value for an unknown client or hostname.
        /// </summary>
        public const string UNKNOWN = "Unknown";

        private const string ENDPOINT_STATUS = "/config/v1/status";
        private const string ENDPOINT_DOC = "/config/v1/doc";
        private const string ENDPOINT_ABOUT = "/config/v1/about";
        private const string ENDPOINT_PROJECT = "/config/v1/project";

        private readonly ILogger<KepwareApiClient> m_logger;
        private readonly HttpClient m_httpClient;

        private bool? m_isConnected = null;
        private bool? m_hasValidCredentials = null;
        private ProductInfo? m_productInfo = null;

        /// <summary>
        /// Gets the logger instance used for logging operations.
        /// </summary>
        /// <remarks>This property provides access to the logger, which can be used to log messages at
        /// various levels. Ensure that the logger is properly initialized before use.</remarks>
        public ILogger Logger => m_logger;


        /// <summary>
        /// Gets the name of the client instance.
        /// </summary>
        public string ClientName { get; }

        /// <summary>
        /// Gets the hostname of the Kepware server the client is connecting to.
        /// </summary>
        public string ClientHostName => m_httpClient.BaseAddress?.Host ?? UNKNOWN;

        /// <summary>
        /// Gets the product information of the connected Kepware server, which includes 
        /// product name and version information. This caches the value during <see cref="KepwareApiClient.TestConnectionAsync(CancellationToken)"/>
        /// and <see cref="KepwareApiClient.GetProductInfoAsync(CancellationToken)"/> and cached for future use. 
        /// It will return null if there is no cached value.
        /// </summary>
        public ProductInfo? ProductInfo => m_productInfo;

        /// <summary>
        /// Gets the client options for the Kepware server connection.
        /// </summary>
        public KepwareApiClientOptions ClientOptions { get; init; }

        /// <summary>
        /// Gets the generic configuration handlers.
        /// </summary>
        /// <remarks> See <see cref="Kepware.Api.ClientHandler.GenericApiHandler"/> for method references.</remarks>
        public GenericApiHandler GenericConfig { get; init; }

        /// <summary>
        /// Gets the project handlers.
        /// </summary>
        /// <remarks> See <see cref="Kepware.Api.ClientHandler.ProjectApiHandler"/> for method references.</remarks>
        public ProjectApiHandler Project { get; init; }

        /// <summary>
        /// Gets the admin handlers.
        /// </summary>
        /// <remarks> See <see cref="Kepware.Api.ClientHandler.AdminApiHandler"/> for method references.</remarks>
        public AdminApiHandler Admin { get; init; }

        /// <summary>
        /// Gets the services handlers.
        /// </summary>
        /// <remarks> See <see cref="Kepware.Api.ClientHandler.ServicesApiHandler"/> for method references.</remarks>
        public ServicesApiHandler ApiServices { get; init; }

        internal HttpClient HttpClient { get { return m_httpClient; } }

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="KepwareApiClient"/> class. This class 
        /// represents a connection to an instance of Kepware. An instance of this is 
        /// used in all configuration calls done.
        /// </summary>
        /// <param name="options">The client options as <see cref="KepwareApiClientOptions"/>.</param>
        /// <param name="loggerFactory">The loggerFactory instance.</param>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance for the connection.</param>
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
            var iotGatewayApiHandler = new IotGatewayApiHandler(this, loggerFactory.CreateLogger<IotGatewayApiHandler>());
            var dataLoggerApiHandler = new DataLoggerApiHandler(this, loggerFactory.CreateLogger<DataLoggerApiHandler>());
            Project = new ProjectApiHandler(this, channelsApiHandler, devicesApiHandler, iotGatewayApiHandler, dataLoggerApiHandler, loggerFactory.CreateLogger<ProjectApiHandler>());
            Admin = new AdminApiHandler(this, loggerFactory.CreateLogger<AdminApiHandler>());
            ApiServices = new ServicesApiHandler(this, loggerFactory.CreateLogger<ServicesApiHandler>());
        }
        #endregion

        #region connection test & product info
        /// <summary>
        /// Tests the connection to the Kepware server and checks if the server runtime is healthy. Also 
        /// validates authentication credentials. 
        /// Uses the /config/v1/status endpoint for health verification.
        /// Uses the /config/v1/doc endpoint to verify credentials.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the connection was successful.</returns>

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (m_isConnected != true) // already connected
                {
                    m_logger.LogInformation("Connecting to {ClientName}-client at {BaseAddress}...", ClientName, m_httpClient.BaseAddress);
                }
                var response = await m_httpClient.GetAsync(ENDPOINT_STATUS, cancellationToken).ConfigureAwait(false);

                // check if the response is successful and contains a healthy status
                // if the response is not successful, we assume the connection is not healthy
                if (!response.IsSuccessStatusCode)
                {
                    m_logger.LogWarning("Failed to connect to {ClientName}-client at {BaseAddress}, Reason: {ReasonPhrase}", ClientName, m_httpClient.BaseAddress, response.ReasonPhrase);
                    ClearConnectionState(); // set connection state to null if we cannot connect
                    return false; // connection failed
                }

                // Deserialize the response content to check the status
                var status = await JsonSerializer.DeserializeAsync(
                        await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                        KepJsonContext.Default.ListApiStatus, cancellationToken)
                        .ConfigureAwait(false);

                // Check if the status is healthy
                if (status?.FirstOrDefault()?.Healthy == false)
                {
                    m_logger.LogWarning("Failed to connect to {ClientName}-client at {BaseAddress}, Reason: {String}", ClientName, m_httpClient.BaseAddress, "Server Status Check Failed");
                    ClearConnectionState(); // set connection state to null if we cannot connect
                    return false; // connection failed
                }

                // If the connection is already healthy, we can return true immediately
                if (m_isConnected == true ) 
                {
                    return true; // connection is healthy
                }

                // Inital connection attempt or a reconnection due to failure,
                // we need to check the product info and credentials
                _ = await GetProductInfoAsync(cancellationToken).ConfigureAwait(false);

                // If we cannot get the product info, we assume the connection is not healthy
                if (m_productInfo == null) 
                {
                    ClearConnectionState(); // set connection state to null if we cannot get product info
                    return false;
                }

                // If we have a valid product info, we can check the credentials
                m_hasValidCredentials = await TestCredentialsAsync(cancellationToken).ConfigureAwait(false);

                // If we do not have valid credentials, we assume the connection is not healthy
                if (m_hasValidCredentials != true)
                {
                    ClearConnectionState(); // set connection state to null if we cannot connect or credentials are invalid
                    m_logger.LogWarning("Connection to {ClientName}-client at {BaseAddress} failed because credentials are invalid", ClientName, m_httpClient.BaseAddress);
                    return false;
                }

                m_logger.LogInformation("Successfully connected to {ClientName}-client: {ProductName} {ProductVersion} on {BaseAddress}", ClientName, m_productInfo?.ProductName, m_productInfo?.ProductVersion, m_httpClient.BaseAddress);

                m_isConnected = true; // set connection state to true if we have a valid product info and credentials
                return m_isConnected.Value; // return true if we have a valid connection

            }
            catch (HttpRequestException httpEx)
            {
                if (m_isConnected == null || m_isConnected == true) // first time after connection change or when connection is lost
                    m_logger.LogWarning("Failed to connect to {ClientName}-client at {BaseAddress}, Reason: {Message}", ClientName, m_httpClient.BaseAddress, httpEx.Message);
            }

            // If we reach this point, we assume the connection is not healthy
            return false;
        }

        /// <summary>
        /// Gets the product information from the Kepware server which includes product name and version information.
        /// Will update the client's product info property, which can be used in other calls to avoid calling the API multiple times for the same information.
        /// Uses the /config/v1/about endpoint
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the product information. <see cref="Kepware.Api.Model.ProductInfo"/></returns>
        public async Task<ProductInfo?> GetProductInfoAsync(CancellationToken cancellationToken = default)
        {
            if (m_productInfo != null)
            {
                // return cached product info if we have it
                return m_productInfo; 
            }

            try
            {
                var response = await m_httpClient.GetAsync(ENDPOINT_ABOUT, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    
                    // Set Product Info for the client if we have a valid response, so we can use it in other calls without needing to call the API again
                    m_productInfo = JsonSerializer.Deserialize(content, KepJsonContext.Default.ProductInfo);
                    return m_productInfo;
                }
                else
                {
                    m_logger.LogWarning("Failed to get product info from endpoint {Endpoint}, Reason: {ReasonPhrase}", "/config/v1/about", response.ReasonPhrase);
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {ClientName}-client at {BaseAddress}: {Message}", ClientName, m_httpClient.BaseAddress, httpEx.Message);
            }
            catch (JsonException jsonEx)
            {
                m_logger.LogWarning(jsonEx, "Failed to parse ProductInfo from {BaseAddress}", m_httpClient.BaseAddress);
            }

            // If we cannot get the product info, we set it to null and return null
            m_productInfo = null;
            return null;
        }

        private async Task<bool> TestCredentialsAsync(CancellationToken cancellationToken = default)
        {
            bool hasValidCredentials = false;
            try
            {
                
                var response = await m_httpClient.GetAsync(ENDPOINT_PROJECT, cancellationToken).ConfigureAwait(false);
                hasValidCredentials = response.IsSuccessStatusCode;
                if (hasValidCredentials)
                {
                    // credentials are valid
                }
                else
                {
                    // log a warning, that we don't have valid credentials
                    m_logger.LogWarning("Failed to connect to {ClientName}-client at {BaseAddress} with valid credentials, Reason: {ReasonPhrase}",
                        ClientName, m_httpClient.BaseAddress, response.ReasonPhrase);
                }
            }
            catch (HttpRequestException httpEx)
            {
                m_logger.LogWarning(httpEx, "Failed to connect to {ClientName}-client at {BaseAddress}", ClientName, m_httpClient.BaseAddress);
            }

            return hasValidCredentials;
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

        #region Private / internal helper methods
        /// <summary>
        /// Clears all client-level connection state and optionally handler caches.
        /// Call this whenever the connection should be considered lost or stale.
        /// </summary>
        /// <param name="clearCredentials">If true also clears cached credential validation state.</param>
        private void ClearConnectionState(bool clearCredentials = true)
        {
            // Clear derived product info and connection flags
            m_productInfo = null;
            m_isConnected = null;

            // Optionally clear credential status so next TestConnection re-evaluates
            if (clearCredentials)
                m_hasValidCredentials = null;

            // Invalidate caches on handlers that keep them
            try
            {
                // GenericConfig may implement an InvalidateCaches method (see suggestion below)
                (GenericConfig as ClientHandler.GenericApiHandler)?.InvalidateCaches();
            }
            catch
            {
                // swallow - defensive: don't throw from a state-clear helper
            }
        }

        /// <summary>
        /// Invoked by Handler, when they receice a http request exception
        /// </summary>
        /// <param name="httpEx"></param>
        internal void OnHttpRequestException(HttpRequestException httpEx)
        {
            ClearConnectionState(false);
        }
        #endregion
    }
}
