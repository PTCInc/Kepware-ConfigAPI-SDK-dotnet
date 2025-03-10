using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class Project
        {
            #region General

            /// <summary>
            /// Title of the project.
            /// </summary>
            public const string Title = "servermain.PROJECT_TITLE";

            /// <summary>
            /// Count of tags identified in the project.
            /// </summary>
            //TODO: Does this need to be moved to non-seralized properties?
            public const string TagsDefined = "servermain.PROJECT_TAGS_DEFINED";

            #endregion

            #region OPC DA

            /// <summary>
            /// Enable or disable OPC DA client connections that support the 1.0 specification.
            /// </summary>
            public const string EnableOpcDa1 = "opcdaserver.PROJECT_OPC_DA_1_ENABLED";

            /// <summary>
            /// Enable or disable OPC DA client connections that support the 2.0 specification.
            /// </summary>
            public const string EnableOpcDa2 = "opcdaserver.PROJECT_OPC_DA_2_ENABLED";

            /// <summary>
            /// Enable or disable OPC DA client connections that support the 3.0 specification.
            /// </summary>
            public const string EnableOpcDa3 = "opcdaserver.PROJECT_OPC_DA_3_ENABLED";

            /// <summary>
            /// Enable or disable address formatting hints available for each communications driver.
            /// </summary>
            public const string OpcShowHintsOnBrowse = "opcdaserver.PROJECT_OPC_SHOW_HINTS_ON_BROWSE";

            /// <summary>
            /// Enable or disable for tag properties to available in the address space.
            /// </summary>
            public const string OpcShowTagPropertiesOnBrowse = "opcdaserver.PROJECT_OPC_SHOW_TAG_PROPERTIES_ON_BROWSE";

            /// <summary>
            /// Time, in seconds, to wait for clients to respond to shutdown notification.
            /// </summary>
            public const string OpcShutdownWaitSec = "opcdaserver.PROJECT_OPC_SHUTDOWN_WAIT_SEC";

            /// <summary>
            /// Time, in seconds, to wait for synchronous request to complete.
            /// </summary>
            public const string OpcSyncRequestWaitSec = "opcdaserver.PROJECT_OPC_SYNC_REQUEST_WAIT_SEC";

            /// <summary>
            /// Enable or disable OPC DA diagnostics data to be logged.
            /// </summary>
            public const string OpcEnableDiags = "opcdaserver.PROJECT_OPC_ENABLE_DIAGS";

            /// <summary>
            /// Maximum number of simultaneous connections to the server the OPC DA interface.
            /// </summary>
            public const string OpcMaxConnections = "opcdaserver.PROJECT_OPC_MAX_CONNECTIONS";

            /// <summary>
            /// Maximum number of simultaneous OPC DA groups.
            /// </summary>
            public const string OpcMaxTagGroups = "opcdaserver.PROJECT_OPC_MAX_TAG_GROUPS";

            /// <summary>
            /// Enable or disable only allows Language IDs that supported by the server.
            /// </summary>
            public const string OpcRejectUnsupportedLangId = "opcdaserver.PROJECT_OPC_REJECT_UNSUPPORTED_LANG_ID";

            /// <summary>
            /// Enable or disable to ignore the deadband setting on OPC DA groups added to the server.
            /// </summary>
            public const string OpcIgnoreDeadbandOnCache = "opcdaserver.PROJECT_OPC_IGNORE_DEADBAND_ON_CACHE";

            /// <summary>
            /// Enable or disable to ignore a filter for an OPC DA client browse request. 
            /// </summary>
            public const string OpcIgnoreBrowseFilter = "opcdaserver.PROJECT_OPC_IGNORE_BROWSE_FILTER";

            /// <summary>
            /// Enable or disable to adhere to the data type coercion behaviors added to the 2.05a specification.
            /// </summary>
            public const string Opc205aDataTypeSupport = "opcdaserver.PROJECT_OPC_205A_DATA_TYPE_SUPPORT";

            /// <summary>
            /// Enable or disable to return a failure if one or more items for a synchronous device read results in a bad quality read.
            /// </summary>
            public const string OpcSyncReadErrorOnBadQuality = "opcdaserver.PROJECT_OPC_SYNC_READ_ERROR_ON_BAD_QUALITY";

            /// <summary>
            /// Enable or disable to return all outstanding initial item updates in a single callback.
            /// </summary>
            public const string OpcReturnInitialUpdatesInSingleCallback = "opcdaserver.PROJECT_OPC_RETURN_INITIAL_UPDATES_IN_SINGLE_CALLBACK";

            /// <summary>
            /// Enable or disable the Locale ID set by the OPC client is used when performing data type conversions to and from string.
            /// </summary>
            public const string OpcRespectClientLangId = "opcdaserver.PROJECT_OPC_RESPECT_CLIENT_LANG_ID";

            /// <summary>
            /// Enable or disable to return S_FALSE in the item error array for items without good quality in data change callback.
            /// </summary>
            public const string OpcCompliantDataChange = "opcdaserver.PROJECT_OPC_COMPLIANT_DATA_CHANGE";

            /// <summary>
            /// Enable or disable to respect the group update rate or ignore it and return data as soon as it becomes available.
            /// </summary>
            public const string OpcIgnoreGroupUpdateRate = "opcdaserver.PROJECT_OPC_IGNORE_GROUP_UPDATE_RATE";

            #endregion

            #region FastDDE/SuiteLink

            /// <summary>
            /// Enable or disable FastDDE/SuiteLink connections to the server.
            /// </summary>
            public const string EnableFastDdeSuiteLink = "wwtoolkitinterface.ENABLED";

            /// <summary>
            /// This server's application name used by FastDDE/SuiteLink client applications.
            /// </summary>
            public const string FastDdeSuiteLinkServiceName = "wwtoolkitinterface.SERVICE_NAME";

            /// <summary>
            /// Update rate for how often new data is sent to FastDDE/SuiteLink client applications.
            /// </summary>
            public const string FastDdeSuiteLinkClientUpdateIntervalMs = "wwtoolkitinterface.CLIENT_UPDATE_INTERVAL_MS";

            #endregion

            #region DDE

            /// <summary>
            /// Enable or disable DDE connections to the server.
            /// </summary>
            public const string EnableDde = "ddeserver.ENABLE";

            /// <summary>
            /// This server's application name for DDE clients.
            /// </summary>
            public const string DdeServiceName = "ddeserver.SERVICE_NAME";

            /// <summary>
            /// Enable or disable support for Advanced DDE format.
            /// </summary>
            public const string DdeAdvancedDde = "ddeserver.ADVANCED_DDE";

            /// <summary>
            /// Enable or disable support for XL Table DDE format.
            /// </summary>
            public const string DdeXlTable = "ddeserver.XLTABLE";

            /// <summary>
            /// Enable or disable support for CF_TEXT DDE format.
            /// </summary>
            public const string DdeCfText = "ddeserver.CF_TEXT";

            /// <summary>
            /// Update rate for how often new batches of DDE data are transferred to client applications.
            /// </summary>
            public const string DdeClientUpdateIntervalMs = "ddeserver.CLIENT_UPDATE_INTERVAL_MS";

            /// <summary>
            /// Timeout for the completion of DDE requests.
            /// </summary>
            public const string DdeRequestTimeoutSec = "ddeserver.REQUEST_TIMEOUT_SEC";

            #endregion

            #region OPC UA

            /// <summary>
            /// Enable or disable the OPC UA server interface to accept client connections.
            /// </summary>
            public const string EnableOpcUa = "uaserverinterface.PROJECT_OPC_UA_ENABLE";

            /// <summary>
            /// Enable or disable OPC UA diagnostics data to be logged.
            /// </summary>
            public const string OpcUaDiagnostics = "uaserverinterface.PROJECT_OPC_UA_DIAGNOSTICS";

            /// <summary>
            /// Allow anonymous login by OPC UA client connections.
            /// </summary>
            public const string OpcUaAnonymousLogin = "uaserverinterface.PROJECT_OPC_UA_ANONYMOUS_LOGIN";

            /// <summary>
            /// The number of simultaneous OPC UA client connections allowed by the server.
            /// </summary>
            public const string OpcUaMaxConnections = "uaserverinterface.PROJECT_OPC_UA_MAX_CONNECTIONS";

            /// <summary>
            /// Minimum session timeout period, in seconds, that OPC UA client is allowed to specify.
            /// </summary>
            public const string OpcUaMinSessionTimeoutSec = "uaserverinterface.PROJECT_OPC_UA_MIN_SESSION_TIMEOUT_SEC";

            /// <summary>
            /// Maximum session timeout period, in seconds, that OPC UA client is allowed to specify.
            /// </summary>
            public const string OpcUaMaxSessionTimeoutSec = "uaserverinterface.PROJECT_OPC_UA_MAX_SESSION_TIMEOUT_SEC";

            /// <summary>
            /// Timeout for OPC UA clients that perform reads/writes on unregistered tags.
            /// </summary>
            public const string OpcUaTagCacheTimeoutSec = "uaserverinterface.PROJECT_OPC_UA_TAG_CACHE_TIMEOUT_SEC";

            /// <summary>
            /// Return tag properties when a OPC UA client browses the server address space.
            /// </summary>
            public const string OpcUaBrowseTagProperties = "uaserverinterface.PROJECT_OPC_UA_BROWSE_TAG_PROPERTIES";

            /// <summary>
            /// Return device addressing hints when a OPC UA client browses the server address space.
            /// </summary>
            public const string OpcUaBrowseAddressHints = "uaserverinterface.PROJECT_OPC_UA_BROWSE_ADDRESS_HINTS";

            /// <summary>
            /// Maximum number of data change notifications queued per monitored item by server.
            /// </summary>
            public const string OpcUaMaxDataQueueSize = "uaserverinterface.PROJECT_OPC_UA_MAX_DATA_QUEUE_SIZE";

            /// <summary>
            /// Maximum number of notifications in the republish queue the server allows per subscription.
            /// </summary>
            public const string OpcUaMaxRetransmitQueueSize = "uaserverinterface.PROJECT_OPC_UA_MAX_RETRANSMIT_QUEUE_SIZE";

            /// <summary>
            /// Maximum number of notifications the server sends per publish.
            /// </summary>
            public const string OpcUaMaxNotificationPerPublish = "uaserverinterface.PROJECT_OPC_UA_MAX_NOTIFICATION_PER_PUBLISH";

            #endregion

            #region OPC AE

            /// <summary>
            /// Enable or disable OPC AE connections to the server.
            /// </summary>
            public const string EnableAeServer = "aeserverinterface.ENABLE_AE_SERVER";

            /// <summary>
            /// Enable or disable OPC AE simple events.
            /// </summary>
            public const string EnableSimpleEvents = "aeserverinterface.ENABLE_SIMPLE_EVENTS";

            /// <summary>
            /// Maximum number of events sent to a OPC AE client in one send call.
            /// </summary>
            public const string MaxSubscriptionBufferSize = "aeserverinterface.MAX_SUBSCRIPTION_BUFFER_SIZE";

            /// <summary>
            /// Minimum time between send calls to a OPC AE client.
            /// </summary>
            public const string MinSubscriptionBufferTimeMs = "aeserverinterface.MIN_SUBSCRIPTION_BUFFER_TIME_MS";

            /// <summary>
            /// Minimum time between keep-alive messages sent from the server to the client.
            /// </summary>
            public const string MinKeepAliveTimeMs = "aeserverinterface.MIN_KEEP_ALIVE_TIME_MS";

            #endregion

            #region OPC HDA

            /// <summary>
            /// Enable or disable OPC HDA connections to the server.
            /// </summary>
            public const string EnableHda = "hdaserver.ENABLE";

            /// <summary>
            /// Enable or disable OPC HDA diagnostics data to be logged.
            /// </summary>
            public const string EnableHdaDiagnostics = "hdaserver.ENABLE_DIAGNOSTICS";

            #endregion

            #region ThingWorx

            /// <summary>
            /// Enable or disable the ThingWorx native interface.
            /// </summary>
            public const string EnableThingWorx = "thingworxinterface.ENABLED";

            /// <summary>
            /// Hostname or IP address of the ThingWorx Platform instance.
            /// </summary>
            public const string ThingWorxHostName = "thingworxinterface.HOSTNAME";

            /// <summary>
            /// Port used to connect to the platform instance.
            /// </summary>
            public const string ThingWorxPort = "thingworxinterface.PORT";

            /// <summary>
            /// Endpoint URL of the platform hosting the websocket server.
            /// </summary>
            public const string ThingWorxResource = "thingworxinterface.RESOURCE";

            /// <summary>
            /// Application key used to authenticate.
            /// </summary>
            public const string ThingWorxAppKey = "thingworxinterface.APPKEY";

            /// <summary>
            /// Enable or disable to trust valid self-signed certificates presented by the server.
            /// </summary>
            public const string ThingWorxAllowSelfSignedCertificate = "thingworxinterface.ALLOW_SELF_SIGNED_CERTIFICATE";

            /// <summary>
            /// Enable or disable to trust all server certificates and completely disable certificate validation.
            /// </summary>
            public const string ThingWorxTrustAllCertificates = "thingworxinterface.TRUST_ALL_CERTIFICATES";

            /// <summary>
            /// Enable or disable SSL/TLS and allow connecting to an insecure endpoint.
            /// </summary>
            public const string ThingWorxDisableEncryption = "thingworxinterface.DISABLE_ENCRYPTION";

            /// <summary>
            /// Maximum number of things that can be bound to this Industrial Gateway.
            /// </summary>
            public const string ThingWorxMaxThingCount = "thingworxinterface.MAX_THING_COUNT";

            /// <summary>
            /// Thing name presented to the Thingworx platform.
            /// </summary>
            public const string ThingWorxThingName = "thingworxinterface.THING_NAME";

            /// <summary>
            /// Minimum rate that updates are sent to the Thingworx platform.
            /// </summary>
            public const string ThingWorxPublishFloorMsec = "thingworxinterface.PUBLISH_FLOOR_MSEC";

            /// <summary>
            /// Enable or disable ThingWorx Advanced Logging.
            /// </summary>
            public const string ThingWorxLoggingEnabled = "thingworxinterface.LOGGING_ENABLED";

            /// <summary>
            /// Sets logging level for Thingworx Advanced Logging.
            /// </summary>
            public const string ThingWorxLogLevel = "thingworxinterface.LOG_LEVEL";

            /// <summary>
            /// Determines the level of detail of each message logged.
            /// </summary>
            public const string ThingWorxVerbose = "thingworxinterface.VERBOSE";

            /// <summary>
            /// Enable or disable Store and Forward for the ThingWorx Native Interface.
            /// </summary>
            public const string ThingWorxStoreAndForwardEnabled = "thingworxinterface.STORE_AND_FORWARD_ENABLED";

            /// <summary>
            /// Directory location for data to be stored for Store and Forward.
            /// </summary>
            public const string ThingWorxStoragePath = "thingworxinterface.STORAGE_PATH";

            /// <summary>
            /// Maximum size of the datastore in which to store updates when offline.
            /// </summary>
            public const string ThingWorxDatastoreMaxSize = "thingworxinterface.DATASTORE_MAXSIZE";

            /// <summary>
            /// Store and Forward Mode upon reconnect.
            /// </summary>
            public const string ThingWorxForwardMode = "thingworxinterface.FORWARD_MODE";

            /// <summary>
            /// Read-only property used to bind a Store and Forward datastore to a project.
            /// </summary>
            // TODO: Is this a non-serialized property?
            public const string ThingWorxDatastoreId = "thingworxinterface.DATASTORE_ID";

            /// <summary>
            /// Specify the minimum amount of time between publishes sent.
            /// </summary>
            public const string ThingWorxDelayBetweenPublishes = "thingworxinterface.DELAY_BETWEEN_PUBLISHES";

            /// <summary>
            /// Maximum number of updates to send in a single publish.
            /// </summary>
            public const string ThingWorxMaxUpdatesPerPublish = "thingworxinterface.MAX_UPDATES_PER_PUBLISH";

            /// <summary>
            /// Enable or disable communication through a proxy server.
            /// </summary>
            public const string ThingWorxProxyEnabled = "thingworxinterface.PROXY_ENABLED";

            /// <summary>
            /// Hostname or IP address of the proxy server.
            /// </summary>
            public const string ThingWorxProxyHost = "thingworxinterface.PROXY_HOST";

            /// <summary>
            /// Port used to connect to the proxy server.
            /// </summary>
            public const string ThingWorxProxyPort = "thingworxinterface.PROXY_PORT";

            /// <summary>
            /// Username used to connect to the proxy server.
            /// </summary>
            public const string ThingWorxProxyUsername = "thingworxinterface.PROXY_USERNAME";

            /// <summary>
            /// Password used to authenticate the username with the proxy server.
            /// </summary>
            public const string ThingWorxProxyPassword = "thingworxinterface.PROXY_PASSWORD";

            #endregion
        }
    }
}