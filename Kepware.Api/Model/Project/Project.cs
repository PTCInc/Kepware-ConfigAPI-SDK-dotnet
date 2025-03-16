using Kepware.Api.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a project in the Kepware configuration
    /// </summary>
    [Endpoint("/config/v1/project")]
    public class Project : DefaultEntity 
    //Updated from BaseEntity to leverage GetUpdateDiff methods for Project Properties updates
    {
        /// <summary>
        /// If this is true the project was loaded by the JsonProjectLoad service (added to Kepware Server v6.17 / Kepware Edge v1.10)
        /// </summary>
        public bool IsLoadedByProjectLoadService { get; internal set; } = false;

        public Project()
        {
            ProjectProperties = new(this);
        }

        #region ProjectProperties

        /// <summary>
        /// Gets or sets the project properties in the project
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public ProjectProperties ProjectProperties { get; }

        #endregion

        /// <summary>
        /// Gets or sets the channels in the project
        /// </summary>
        [YamlIgnore]
        [JsonPropertyName("channels")]
        [JsonPropertyOrder(100)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ChannelCollection? Channels { get; set; }

        /// <summary>
        /// Recursively cleans up the project and all its children
        /// </summary>
        /// <param name="defaultValueProvider"></param>
        /// <param name="blnRemoveProjectId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task Cleanup(IKepwareDefaultValueProvider defaultValueProvider, bool blnRemoveProjectId = false, CancellationToken cancellationToken = default)
        {
            await base.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);


            if (Channels != null)
            {
                foreach (var channel in Channels)
                {
                    await channel.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<Project> CloneAsync(CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, this, KepJsonContext.Default.Project, cancellationToken).ConfigureAwait(false);
            stream.Position = 0;

            return await JsonSerializer.DeserializeAsync(stream, KepJsonContext.Default.Project, cancellationToken).ConfigureAwait(false) ??
                throw new InvalidOperationException("CloneAsync failed");
        }

    }

    #region Enums

    /// <summary>
    /// Defines the ThingWorx native interface logging level modes. 
    /// Determines that amount of information logged. Set to Trace to generate the most detailed output.
    /// </summary>
    public enum ThingwWorxLoggingLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Audit = 6,
    }

    /// <summary>
    /// The maximum size of the datastore in which to store updates when offline. 
    /// Changing this property causes the interface to restart and deletes the current datastore.
    /// </summary>
    public enum ThingWorxDataStoreMaxSize
    {
        Size128MB = 128,
        Size256MB = 256,
        Size512MB = 512,
        Size1GB = 1024,
        Size2GB = 2048,
        Size4GB = 4096,
        Size8GB = 8192,
        Size16GB = 16384
    }

    /// <summary>
    /// Specify the Forward Mode to control which updates are sent to the platform upon reconnect.
    /// </summary>
    public enum ThingWorxForwardMode
    {
        Active = 0,
        OnHold = 1
    }

    #endregion

    /// <summary>
    /// Represents the project properties in the Kepware configuration
    /// </summary>
    public partial class ProjectProperties(Project project)
    // Inherit Named Entity type to ensure ProjectID management when updating properties.
    // Updates for these properties need ProjectID or overrides
    {

        #region General
        /// <summary>
        /// Title of the project.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Title
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.Title);
            set => project.SetDynamicProperty(Properties.ProjectSettings.Title, value);
        }

        /// <summary>
        /// Count of tags identified in the project.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? TagsDefined
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.TagsDefined);
        }

        #endregion

        #region OPC DA
        /// <summary>
        /// Enable or disable OPC DA client connections that support the 1.0 specification.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableOpcDa1
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.EnableOpcDa1);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.EnableOpcDa1, value);
        }

        /// <summary>
        /// Enable or disable OPC DA client connections that support the 2.0 specification.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableOpcDa2
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.EnableOpcDa2);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.EnableOpcDa2, value);
        }

        /// <summary>
        /// Enable or disable OPC DA client connections that support the 3.0 specification.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableOpcDa3
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.EnableOpcDa3);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.EnableOpcDa3, value);
        }

        /// <summary>
        /// Enable or disable address formatting hints available for each communications driver.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaShowHintsOnBrowse
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.ShowHintsOnBrowse);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.ShowHintsOnBrowse, value);
        }

        /// <summary>
        /// Enable or disable for tag properties to available in the address space.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaShowTagPropertiesOnBrowse
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.ShowTagPropertiesOnBrowse);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.ShowTagPropertiesOnBrowse, value);
        }

        /// <summary>
        /// Time, in seconds, to wait for clients to respond to shutdown notification.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcDaShutdownWaitSec
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcDa.ShutdownWaitSec);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.ShutdownWaitSec, value);
        }

        /// <summary>
        /// Time, in seconds, to wait for synchronous request to complete.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcDaSyncRequestTimeoutSec
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcDa.SyncRequestTimeoutSec);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.SyncRequestTimeoutSec, value);
        }

        /// <summary>
        /// Enable or disable OPC DA diagnostics data to be logged.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaEnableDiagnostics
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.EnableDiagnostics);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.EnableDiagnostics, value);
        }

        /// <summary>
        /// Maximum number of simultaneous connections to the server the OPC DA interface.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcDaMaxConnections
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcDa.MaxConnections);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.MaxConnections, value);
        }

        /// <summary>
        /// Maximum number of simultaneous OPC DA groups.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcDaMaxTagGroups
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcDa.MaxOpcDaGroups);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.MaxOpcDaGroups, value);
        }

        /// <summary>
        /// Enable or disable only allows Language IDs that supported by the server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaRejectUnsupportedLangId
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.RejectUnsupportedLangId);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.RejectUnsupportedLangId, value);
        }

        /// <summary>
        /// Enable or disable to ignore the deadband setting on OPC DA groups added to the server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaIgnoreDeadbandOnCache
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.IgnoreDeadbandOnCache);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.IgnoreDeadbandOnCache, value);
        }

        /// <summary>
        /// Enable or disable to ignore a filter for an OPC DA client browse request.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaIgnoreBrowseFilter
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.IgnoreBrowseFilter);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.IgnoreBrowseFilter, value);
        }

        /// <summary>
        /// Enable or disable to adhere to the data type coercion behaviors added to the 2.05a specification.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDa205aDataTypeSupport
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.OpcDa205aDataTypeSupport);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.OpcDa205aDataTypeSupport, value);
        }

        /// <summary>
        /// Enable or disable to return a failure if one or more items for a synchronous device read results in a bad quality read.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaSyncReadErrorOnBadQuality
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.FailOnBadQuality);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.FailOnBadQuality, value);
        }

        /// <summary>
        /// Enable or disable to return all outstanding initial item updates in a single callback.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaReturnInitialUpdatesInSingleCallback
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.ReturnInitialUpdatesInSingleCallback);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.ReturnInitialUpdatesInSingleCallback, value);
        }

        /// <summary>
        /// Enable or disable the Locale ID set by the OPC client is used when performing data type conversions to and from string.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaRespectClientLangId
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.RespectClientLangId);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.RespectClientLangId, value);
        }

        /// <summary>
        /// Enable or disable to return S_FALSE in the item error array for items without good quality in data change callback.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaCompliantDataChange
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.OpcCompliantDataChange);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.OpcCompliantDataChange, value);
        }

        /// <summary>
        /// Enable or disable to respect the group update rate or ignore it and return data as soon as it becomes available.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcDaIgnoreGroupUpdateRate
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcDa.IgnoreGroupUpdateRate);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcDa.IgnoreGroupUpdateRate, value);
        }

        #endregion

        #region FastDDE/SuiteLink
        /// <summary>
        /// Enable or disable FastDDE/SuiteLink connections to the server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableFastDdeSuiteLink
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.FastDdeSuiteLink.Enable);
            set => project.SetDynamicProperty(Properties.ProjectSettings.FastDdeSuiteLink.Enable, value);
        }

        /// <summary>
        /// This server's application name used by FastDDE/SuiteLink client applications.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? FastDdeSuiteLinkApplicationName
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.FastDdeSuiteLink.ApplicationName);
            set => project.SetDynamicProperty(Properties.ProjectSettings.FastDdeSuiteLink.ApplicationName, value);
        }

        /// <summary>
        /// Update rate for how often new data is sent to FastDDE/SuiteLink client applications.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? FastDdeSuiteLinkClientUpdateIntervalMs
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.FastDdeSuiteLink.ClientUpdateIntervalMs);
            set => project.SetDynamicProperty(Properties.ProjectSettings.FastDdeSuiteLink.ClientUpdateIntervalMs, value);
        }

        /// <summary>
        /// Enable or disable DDE connections to the server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableDde
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.Dde.EnableDde);
            set => project.SetDynamicProperty(Properties.ProjectSettings.Dde.EnableDde, value);
        }

        /// <summary>
        /// This server's application name for DDE clients.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? DdeServiceName
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.Dde.ServiceName);
            set => project.SetDynamicProperty(Properties.ProjectSettings.Dde.ServiceName, value);
        }

        /// <summary>
        /// Enable or disable support for Advanced DDE format.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableDdeAdvancedDde
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.Dde.EnableAdvancedDde);
            set => project.SetDynamicProperty(Properties.ProjectSettings.Dde.EnableAdvancedDde, value);
        }

        /// <summary>
        /// Enable or disable support for XL Table DDE format.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableDdeXlTable
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.Dde.EnableXlTable);
            set => project.SetDynamicProperty(Properties.ProjectSettings.Dde.EnableXlTable, value);
        }

        /// <summary>
        /// Enable or disable support for CF_TEXT DDE format.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableDdeCfText
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.Dde.EnableCfText);
            set => project.SetDynamicProperty(Properties.ProjectSettings.Dde.EnableCfText, value);
        }

        /// <summary>
        /// Update rate for how often new batches of DDE data are transferred to client applications.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? DdeClientUpdateIntervalMs
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.Dde.ClientUpdateIntervalMs);
            set => project.SetDynamicProperty(Properties.ProjectSettings.Dde.ClientUpdateIntervalMs, value);
        }

        /// <summary>
        /// Timeout for the completion of DDE requests.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? DdeRequestTimeoutSec
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.Dde.RequestTimeoutSec);
            set => project.SetDynamicProperty(Properties.ProjectSettings.Dde.RequestTimeoutSec, value);
        }

        #endregion

        #region OPC UA

        /// <summary>
        /// Enable or disable the OPC UA server interface to accept client connections.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableOpcUa
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcUa.Enable);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.Enable, value);
        }

        /// <summary>
        /// Enable or disable OPC UA diagnostics data to be logged.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcUaEnableDiagnostics
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcUa.EnableDiagnostics);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.EnableDiagnostics, value);
        }

        /// <summary>
        /// Allow anonymous login by OPC UA client connections.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcUaAllowAnonymousLogin
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcUa.AllowAnonymousLogin);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.AllowAnonymousLogin, value);
        }

        /// <summary>
        /// The number of simultaneous OPC UA client connections allowed by the server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcUaMaxConnections
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcUa.MaxConnections);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.MaxConnections, value);
        }

        /// <summary>
        /// Minimum session timeout period, in seconds, that OPC UA client is allowed to specify.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcUaMinSessionTimeoutSec
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcUa.MinSessionTimeoutSec);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.MinSessionTimeoutSec, value);
        }

        /// <summary>
        /// Maximum session timeout period, in seconds, that OPC UA client is allowed to specify.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcUaMaxSessionTimeoutSec
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcUa.MaxSessionTimeoutSec);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.MaxSessionTimeoutSec, value);
        }

        /// <summary>
        /// Timeout for OPC UA clients that perform reads/writes on unregistered tags.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcUaTagCacheTimeoutSec
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcUa.TagCacheTimeoutSec);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.TagCacheTimeoutSec, value);
        }

        /// <summary>
        /// Return tag properties when a OPC UA client browses the server address space.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcUaShowTagPropertiesOnBrowse
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcUa.ShowTagPropertiesOnBrowse);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.ShowTagPropertiesOnBrowse, value);
        }

        /// <summary>
        /// Return device addressing hints when a OPC UA client browses the server address space.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcUaShowHintsOnBrowse
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcUa.ShowHintsOnBrowse);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.ShowHintsOnBrowse, value);
        }

        /// <summary>
        /// Maximum number of data change notifications queued per monitored item by server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcUaMaxDataQueueSize
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcUa.MaxDataQueueSize);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.MaxDataQueueSize, value);
        }

        /// <summary>
        /// Maximum number of notifications in the republish queue the server allows per subscription.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcUaMaxRetransmitQueueSize
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcUa.MaxRetransmitQueueSize);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.MaxRetransmitQueueSize, value);
        }

        /// <summary>
        /// Maximum number of notifications the server sends per publish.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? OpcUaMaxNotificationPerPublish
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcUa.MaxNotificationPerPublish);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcUa.MaxNotificationPerPublish, value);
        }
        #endregion

        #region OPC AE

        /// <summary>
        /// Enable or disable OPC AE connections to the server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableAeServer
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcAe.Enable);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcAe.Enable, value);
        }

        /// <summary>
        /// Enable or disable OPC AE simple events.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableSimpleEvents
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcAe.EnableSimpleEvents);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcAe.EnableSimpleEvents, value);
        }

        /// <summary>
        /// Maximum number of events sent to a OPC AE client in one send call.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? MaxSubscriptionBufferSize
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcAe.MaxSubscriptionBufferSize);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcAe.MaxSubscriptionBufferSize, value);
        }

        /// <summary>
        /// Minimum time between send calls to a OPC AE client.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? MinSubscriptionBufferTimeMs
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcAe.MinSubscriptionBufferTimeMs);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcAe.MinSubscriptionBufferTimeMs, value);
        }

        /// <summary>
        /// Minimum time between keep-alive messages sent from the server to the client.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? MinKeepAliveTimeMs
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.OpcAe.MinKeepAliveTimeMs);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcAe.MinKeepAliveTimeMs, value);
        }

        #endregion

        #region OPC HDA

        /// <summary>
        /// Enable or disable OPC HDA connections to the server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableHda
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcHda.Enable);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcHda.Enable, value);
        }

        /// <summary>
        /// Enable or disable OPC HDA diagnostics data to be logged.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? OpcHdaEnableDiagnostics
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.OpcHda.EnableDiagnostics);
            set => project.SetDynamicProperty(Properties.ProjectSettings.OpcHda.EnableDiagnostics, value);
        }

        #endregion

        #region ThingWorx

        /// <summary>
        /// Enable or disable the ThingWorx native interface.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? EnableThingWorx
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.ThingWorx.Enable);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.Enable, value);
        }

        /// <summary>
        /// Hostname or IP address of the ThingWorx Platform instance.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ThingWorxHostName
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.ThingWorx.HostName);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.HostName, value);
        }

        /// <summary>
        /// Port used to connect to the platform instance.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ThingWorxPort
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.ThingWorx.Port);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.Port, value);
        }

        /// <summary>
        /// Endpoint URL of the platform hosting the websocket server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ThingWorxResourceUrl
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.ThingWorx.ResourceUrl);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.ResourceUrl, value);
        }

        /// <summary>
        /// Application key used to authenticate.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ThingWorxApplicationKey
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.ThingWorx.ApplicationKey);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.ApplicationKey, value);
        }

        /// <summary>
        /// Enable or disable to trust valid self-signed certificates presented by the server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ThingWorxTrustSelfSignedCertificate
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.ThingWorx.TrustSelfSignedCertificate);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.TrustSelfSignedCertificate, value);
        }

        /// <summary>
        /// Enable or disable to trust all server certificates and completely disable certificate validation.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ThingWorxTrustAllCertificates
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.ThingWorx.TrustAllCertificates);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.TrustAllCertificates, value);
        }

        /// <summary>
        /// Enable or disable SSL/TLS and allow connecting to an insecure endpoint.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ThingWorxDisableEncryption
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.ThingWorx.DisableEncryption);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.DisableEncryption, value);
        }

        /// <summary>
        /// Maximum number of things that can be bound to this Industrial Gateway.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ThingWorxMaxThingCount
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.ThingWorx.MaxThingCount);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.MaxThingCount, value);
        }

        /// <summary>
        /// Thing name presented to the Thingworx platform.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ThingWorxThingName
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.ThingWorx.ThingName);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.ThingName, value);
        }

        /// <summary>
        /// Minimum rate that updates are sent to the Thingworx platform.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ThingWorxPublishFloorMs
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.ThingWorx.PublishFloorMs);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.PublishFloorMs, value);
        }

        /// <summary>
        /// Enable or disable ThingWorx Advanced Logging.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ThingWorxLoggingEnabled
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.ThingWorx.LoggingEnabled);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.LoggingEnabled, value);
        }

        /// <summary>
        /// Sets logging level for Thingworx Advanced Logging.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public ThingwWorxLoggingLevel? ThingWorxLoggingLevel
        {
            get => (ThingwWorxLoggingLevel?)project.GetDynamicProperty<int>(Properties.ProjectSettings.ThingWorx.LoggingLevel);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.LoggingLevel, (int?)value);
        }

        /// <summary>
        /// Determines the level of detail of each message logged.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ThingWorxLogVerbose
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.ThingWorx.LoggingVerbose);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.LoggingVerbose, value);
        }

        /// <summary>
        /// Enable or disable Store and Forward for the ThingWorx Native Interface.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ThingWorxStoreAndForwardEnabled
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.ThingWorx.StoreAndForwardEnabled);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.StoreAndForwardEnabled, value);
        }

        /// <summary>
        /// Directory location for data to be stored for Store and Forward.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ThingWorxStoreAndForwardStoragePath
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.ThingWorx.StoragePath);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.StoragePath, value);
        }

        /// <summary>
        /// Maximum size of the datastore in which to store updates when offline.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public ThingWorxDataStoreMaxSize? ThingWorxMaxDatastoreSize
        {
            get => (ThingWorxDataStoreMaxSize?)project.GetDynamicProperty<int>(Properties.ProjectSettings.ThingWorx.MaxDatastoreSize);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.MaxDatastoreSize, (int?)value);
        }

        /// <summary>
        /// Store and Forward Mode upon reconnect.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public ThingWorxForwardMode? ThingWorxStoreAndForwardMode
        {
            get => (ThingWorxForwardMode?)project.GetDynamicProperty<int>(Properties.ProjectSettings.ThingWorx.ForwardMode);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.ForwardMode, (int?)value);
        }

        /// <summary>
        /// Specify the minimum amount of time between publishes sent.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ThingWorxDelayBetweenPublishes
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.ThingWorx.DelayBetweenPublishes);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.DelayBetweenPublishes, value);
        }

        /// <summary>
        /// Maximum number of updates to send in a single publish.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ThingWorxMaxUpdatesPerPublish
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.ThingWorx.MaxUpdatesPerPublish);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.MaxUpdatesPerPublish, value);
        }

        /// <summary>
        /// Enable or disable communication through a proxy server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ThingWorxProxyEnabled
        {
            get => project.GetDynamicProperty<bool>(Properties.ProjectSettings.ThingWorx.ProxyEnabled);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.ProxyEnabled, value);
        }

        /// <summary>
        /// Hostname or IP address of the proxy server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ThingWorxProxyHost
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.ThingWorx.ProxyHost);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.ProxyHost, value);
        }

        /// <summary>
        /// Port used to connect to the proxy server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ThingWorxProxyPort
        {
            get => project.GetDynamicProperty<int>(Properties.ProjectSettings.ThingWorx.ProxyPort);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.ProxyPort, value);
        }

        /// <summary>
        /// Username used to connect to the proxy server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ThingWorxProxyUsername
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.ThingWorx.ProxyUsername);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.ProxyUsername, value);
        }

        /// <summary>
        /// Password used to authenticate the username with the proxy server.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ThingWorxProxyPassword
        {
            get => project.GetDynamicProperty<string>(Properties.ProjectSettings.ThingWorx.ProxyPassword);
            set => project.SetDynamicProperty(Properties.ProjectSettings.ThingWorx.ProxyPassword, value);
        }

        #endregion
    }
}
