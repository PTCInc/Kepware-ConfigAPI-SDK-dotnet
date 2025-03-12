using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class ProjectSettings
        {
            #region General

            /// <summary>
            /// Title of the project.
            /// </summary>
            public const string Title = "servermain.PROJECT_TITLE";

            /// <summary>
            /// Count of tags identified in the project.
            /// </summary>
            // TODO: Does this need to be moved to non-seralized properties?
            public const string TagsDefined = "servermain.PROJECT_TAGS_DEFINED";

            #endregion

            #region OPC DA

            /// <summary>
            /// Contains constants related to OPC DA server properties.
            /// </summary>
            public static class OpcDa
            {
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
                public const string ShowHintsOnBrowse = "opcdaserver.PROJECT_OPC_SHOW_HINTS_ON_BROWSE";

                /// <summary>
                /// Enable or disable for tag properties to available in the address space.
                /// </summary>
                public const string ShowTagPropertiesOnBrowse = "opcdaserver.PROJECT_OPC_SHOW_TAG_PROPERTIES_ON_BROWSE";

                /// <summary>
                /// Time, in seconds, to wait for clients to respond to shutdown notification.
                /// </summary>
                public const string ShutdownWaitSec = "opcdaserver.PROJECT_OPC_SHUTDOWN_WAIT_SEC";

                /// <summary>
                /// Time, in seconds, to wait for synchronous request to complete.
                /// </summary>
                public const string SyncRequestTimeoutSec = "opcdaserver.PROJECT_OPC_SYNC_REQUEST_WAIT_SEC";

                /// <summary>
                /// Enable or disable OPC DA diagnostics data to be logged.
                /// </summary>
                public const string EnableDiagnostics = "opcdaserver.PROJECT_OPC_ENABLE_DIAGS";

                /// <summary>
                /// Maximum number of simultaneous connections to the server the OPC DA interface.
                /// </summary>
                public const string MaxConnections = "opcdaserver.PROJECT_OPC_MAX_CONNECTIONS";

                /// <summary>
                /// Maximum number of simultaneous OPC DA groups.
                /// </summary>
                public const string MaxOpcDaGroups = "opcdaserver.PROJECT_OPC_MAX_TAG_GROUPS";

                /// <summary>
                /// Enable or disable only allows Language IDs that supported by the server.
                /// </summary>
                public const string RejectUnsupportedLangId = "opcdaserver.PROJECT_OPC_REJECT_UNSUPPORTED_LANG_ID";

                /// <summary>
                /// Enable or disable to ignore the deadband setting on OPC DA groups added to the server.
                /// </summary>
                public const string IgnoreDeadbandOnCache = "opcdaserver.PROJECT_OPC_IGNORE_DEADBAND_ON_CACHE";

                /// <summary>
                /// Enable or disable to ignore a filter for an OPC DA client browse request. 
                /// </summary>
                public const string IgnoreBrowseFilter = "opcdaserver.PROJECT_OPC_IGNORE_BROWSE_FILTER";

                /// <summary>
                /// Enable or disable to adhere to the data type coercion behaviors added to the 2.05a specification.
                /// </summary>
                public const string OpcDa205aDataTypeSupport = "opcdaserver.PROJECT_OPC_205A_DATA_TYPE_SUPPORT";

                /// <summary>
                /// Enable or disable to return a failure if one or more items for a synchronous device read results in a bad quality read.
                /// </summary>
                public const string FailOnBadQuality = "opcdaserver.PROJECT_OPC_SYNC_READ_ERROR_ON_BAD_QUALITY";

                /// <summary>
                /// Enable or disable to return all outstanding initial item updates in a single callback.
                /// </summary>
                public const string ReturnInitialUpdatesInSingleCallback = "opcdaserver.PROJECT_OPC_RETURN_INITIAL_UPDATES_IN_SINGLE_CALLBACK";

                /// <summary>
                /// Enable or disable the Locale ID set by the OPC client is used when performing data type conversions to and from string.
                /// </summary>
                public const string RespectClientLangId = "opcdaserver.PROJECT_OPC_RESPECT_CLIENT_LANG_ID";

                /// <summary>
                /// Enable or disable to return S_FALSE in the item error array for items without good quality in data change callback.
                /// </summary>
                public const string OpcCompliantDataChange = "opcdaserver.PROJECT_OPC_COMPLIANT_DATA_CHANGE";

                /// <summary>
                /// Enable or disable to respect the group update rate or ignore it and return data as soon as it becomes available.
                /// </summary>
                public const string IgnoreGroupUpdateRate = "opcdaserver.PROJECT_OPC_IGNORE_GROUP_UPDATE_RATE";
            }

            #endregion

            #region FastDDE/SuiteLink

            /// <summary>
            /// Contains constants related to FastDDE/SuiteLink server properties.
            /// </summary>
            public static class FastDdeSuiteLink
            {
                /// <summary>
                /// Enable or disable FastDDE/SuiteLink connections to the server.
                /// </summary>
                public const string Enable = "wwtoolkitinterface.ENABLED";

                /// <summary>
                /// This server's application name used by FastDDE/SuiteLink client applications.
                /// </summary>
                public const string ApplicationName = "wwtoolkitinterface.SERVICE_NAME";

                /// <summary>
                /// Update rate for how often new data is sent to FastDDE/SuiteLink client applications.
                /// </summary>
                public const string ClientUpdateIntervalMs = "wwtoolkitinterface.CLIENT_UPDATE_INTERVAL_MS";

            }

            #endregion

            #region DDE

            /// <summary>
            /// Contains constants related to DDE server properties.
            /// </summary>
            public static class Dde
            {
                /// <summary>
                /// Enable or disable DDE connections to the server.
                /// </summary>
                public const string EnableDde = "ddeserver.ENABLE";

                /// <summary>
                /// This server's application name for DDE clients.
                /// </summary>
                public const string ServiceName = "ddeserver.SERVICE_NAME";

                /// <summary>
                /// Enable or disable support for Advanced DDE format.
                /// </summary>
                public const string EnableAdvancedDde = "ddeserver.ADVANCED_DDE";

                /// <summary>
                /// Enable or disable support for XL Table DDE format.
                /// </summary>
                public const string EnableXlTable = "ddeserver.XLTABLE";

                /// <summary>
                /// Enable or disable support for CF_TEXT DDE format.
                /// </summary>
                public const string EnableCfText = "ddeserver.CF_TEXT";

                /// <summary>
                /// Update rate for how often new batches of DDE data are transferred to client applications.
                /// </summary>
                public const string ClientUpdateIntervalMs = "ddeserver.CLIENT_UPDATE_INTERVAL_MS";

                /// <summary>
                /// Timeout for the completion of DDE requests.
                /// </summary>
                public const string RequestTimeoutSec = "ddeserver.REQUEST_TIMEOUT_SEC";
            }

            #endregion

            #region OPC UA

            /// <summary>
            /// Contains constants related to OPC UA server properties.
            /// </summary>
            public static class OpcUa
            {
                /// <summary>
                /// Enable or disable the OPC UA server interface to accept client connections.
                /// </summary>
                public const string Enable = "uaserverinterface.PROJECT_OPC_UA_ENABLE";

                /// <summary>
                /// Enable or disable OPC UA diagnostics data to be logged.
                /// </summary>
                public const string EnableDiagnostics = "uaserverinterface.PROJECT_OPC_UA_DIAGNOSTICS";

                /// <summary>
                /// Allow anonymous login by OPC UA client connections.
                /// </summary>
                public const string AllowAnonymousLogin = "uaserverinterface.PROJECT_OPC_UA_ANONYMOUS_LOGIN";

                /// <summary>
                /// The number of simultaneous OPC UA client connections allowed by the server.
                /// </summary>
                public const string MaxConnections = "uaserverinterface.PROJECT_OPC_UA_MAX_CONNECTIONS";

                /// <summary>
                /// Minimum session timeout period, in seconds, that OPC UA client is allowed to specify.
                /// </summary>
                public const string MinSessionTimeoutSec = "uaserverinterface.PROJECT_OPC_UA_MIN_SESSION_TIMEOUT_SEC";

                /// <summary>
                /// Maximum session timeout period, in seconds, that OPC UA client is allowed to specify.
                /// </summary>
                public const string MaxSessionTimeoutSec = "uaserverinterface.PROJECT_OPC_UA_MAX_SESSION_TIMEOUT_SEC";

                /// <summary>
                /// Timeout for OPC UA clients that perform reads/writes on unregistered tags.
                /// </summary>
                public const string TagCacheTimeoutSec = "uaserverinterface.PROJECT_OPC_UA_TAG_CACHE_TIMEOUT_SEC";

                /// <summary>
                /// Return tag properties when a OPC UA client browses the server address space.
                /// </summary>
                public const string ShowTagPropertiesOnBrowse = "uaserverinterface.PROJECT_OPC_UA_BROWSE_TAG_PROPERTIES";

                /// <summary>
                /// Return device addressing hints when a OPC UA client browses the server address space.
                /// </summary>
                public const string ShowHintsOnBrowse = "uaserverinterface.PROJECT_OPC_UA_BROWSE_ADDRESS_HINTS";

                /// <summary>
                /// Maximum number of data change notifications queued per monitored item by server.
                /// </summary>
                public const string MaxDataQueueSize = "uaserverinterface.PROJECT_OPC_UA_MAX_DATA_QUEUE_SIZE";

                /// <summary>
                /// Maximum number of notifications in the republish queue the server allows per subscription.
                /// </summary>
                public const string MaxRetransmitQueueSize = "uaserverinterface.PROJECT_OPC_UA_MAX_RETRANSMIT_QUEUE_SIZE";

                /// <summary>
                /// Maximum number of notifications the server sends per publish.
                /// </summary>
                public const string MaxNotificationPerPublish = "uaserverinterface.PROJECT_OPC_UA_MAX_NOTIFICATION_PER_PUBLISH";

            }

            #endregion

            #region OPC AE

            public static class OpcAe
            {
                /// <summary>
                /// Enable or disable OPC AE connections to the server.
                /// </summary>
                public const string Enable = "aeserverinterface.ENABLE_AE_SERVER";

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

            }

            #endregion

            #region OPC HDA

            /// <summary>
            /// Contains constants related to OPC HDA server properties.
            /// </summary>
            public static class OpcHda
            {
                /// <summary>
                /// Enable or disable OPC HDA connections to the server.
                /// </summary>
                public const string Enable = "hdaserver.ENABLE";

                /// <summary>
                /// Enable or disable OPC HDA diagnostics data to be logged.
                /// </summary>
                public const string EnableDiagnostics = "hdaserver.ENABLE_DIAGNOSTICS";
            }


            #endregion

            #region ThingWorx

            public static class ThingWorx
            {
                /// <summary>
                /// Enable or disable the ThingWorx native interface.
                /// </summary>
                public const string Enable = "thingworxinterface.ENABLED";

                /// <summary>
                /// Hostname or IP address of the ThingWorx Platform instance.
                /// </summary>
                public const string HostName = "thingworxinterface.HOSTNAME";

                /// <summary>
                /// Port used to connect to the platform instance.
                /// </summary>
                public const string Port = "thingworxinterface.PORT";

                /// <summary>
                /// Endpoint URL of the platform hosting the websocket server.
                /// </summary>
                public const string ResourceUrl = "thingworxinterface.RESOURCE";

                /// <summary>
                /// Application key used to authenticate.
                /// </summary>
                public const string ApplicationKey = "thingworxinterface.APPKEY";

                /// <summary>
                /// Enable or disable to trust valid self-signed certificates presented by the server.
                /// </summary>
                public const string TrustSelfSignedCertificate = "thingworxinterface.ALLOW_SELF_SIGNED_CERTIFICATE";

                /// <summary>
                /// Enable or disable to trust all server certificates and completely disable certificate validation.
                /// </summary>
                public const string TrustAllCertificates = "thingworxinterface.TRUST_ALL_CERTIFICATES";

                /// <summary>
                /// Enable or disable SSL/TLS and allow connecting to an insecure endpoint.
                /// </summary>
                public const string DisableEncryption = "thingworxinterface.DISABLE_ENCRYPTION";

                /// <summary>
                /// Maximum number of things that can be bound to this Industrial Gateway.
                /// </summary>
                public const string MaxThingCount = "thingworxinterface.MAX_THING_COUNT";

                /// <summary>
                /// Thing name presented to the Thingworx platform.
                /// </summary>
                public const string ThingName = "thingworxinterface.THING_NAME";

                /// <summary>
                /// Minimum rate that updates are sent to the Thingworx platform.
                /// </summary>
                public const string PublishFloorMs = "thingworxinterface.PUBLISH_FLOOR_MSEC";

                /// <summary>
                /// Enable or disable ThingWorx Advanced Logging.
                /// </summary>
                public const string LoggingEnabled = "thingworxinterface.LOGGING_ENABLED";

                /// <summary>
                /// Sets logging level for Thingworx Advanced Logging.
                /// </summary>
                public const string LoggingLevel = "thingworxinterface.LOG_LEVEL";

                /// <summary>
                /// Determines the level of detail of each message logged.
                /// </summary>
                public const string LoggingVerbose = "thingworxinterface.VERBOSE";

                /// <summary>
                /// Enable or disable Store and Forward for the ThingWorx Native Interface.
                /// </summary>
                public const string StoreAndForwardEnabled = "thingworxinterface.STORE_AND_FORWARD_ENABLED";

                /// <summary>
                /// Directory location for data to be stored for Store and Forward.
                /// </summary>
                public const string StoragePath = "thingworxinterface.STORAGE_PATH";

                /// <summary>
                /// Maximum size of the datastore in which to store updates when offline.
                /// </summary>
                public const string MaxDatastoreSize = "thingworxinterface.DATASTORE_MAXSIZE";

                /// <summary>
                /// Store and Forward Mode upon reconnect.
                /// </summary>
                public const string ForwardMode = "thingworxinterface.FORWARD_MODE";

                /// <summary>
                /// Specify the minimum amount of time between publishes sent.
                /// </summary>
                public const string DelayBetweenPublishes = "thingworxinterface.DELAY_BETWEEN_PUBLISHES";

                /// <summary>
                /// Maximum number of updates to send in a single publish.
                /// </summary>
                public const string MaxUpdatesPerPublish = "thingworxinterface.MAX_UPDATES_PER_PUBLISH";

                /// <summary>
                /// Enable or disable communication through a proxy server.
                /// </summary>
                public const string ProxyEnabled = "thingworxinterface.PROXY_ENABLED";

                /// <summary>
                /// Hostname or IP address of the proxy server.
                /// </summary>
                public const string ProxyHost = "thingworxinterface.PROXY_HOST";

                /// <summary>
                /// Port used to connect to the proxy server.
                /// </summary>
                public const string ProxyPort = "thingworxinterface.PROXY_PORT";

                /// <summary>
                /// Username used to connect to the proxy server.
                /// </summary>
                public const string ProxyUsername = "thingworxinterface.PROXY_USERNAME";

                /// <summary>
                /// Password used to authenticate the username with the proxy server.
                /// </summary>
                public const string ProxyPassword = "thingworxinterface.PROXY_PASSWORD";
            }
            

            #endregion
        }
    }
}