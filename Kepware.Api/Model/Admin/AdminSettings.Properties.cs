using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class AdminSettings
        {
            /// <summary>
            /// Contains constants related to licensing settings.
            /// </summary>
            public static class Licensing
            {
                /// <summary>
                /// The licensing server name or IP address.
                /// </summary>
                public const string ServerName = "libadminsettings.LICENSING_SERVER_NAME";

                /// <summary>
                /// Enables or disables the licensing server connection.
                /// </summary>
                public const string ServerEnable = "libadminsettings.LICENSING_SERVER_ENABLE";

                /// <summary>
                /// The port number used to connect to license server for non-TLS connections
                /// </summary>
                public const string ServerPort = "libadminsettings.LICENSING_SERVER_PORT";

                /// <summary>
                /// The TCP port for secure licensing server communication.
                /// </summary>
                public const string ServerSslPort = "libadminsettings.LICENSING_SERVER_SSL_PORT";

                /// <summary>
                /// Allows or disallows insecure (non-TLS) connections to the licensing server.
                /// </summary>
                public const string AllowInsecureComms = "libadminsettings.LICENSING_SERVER_ALLOW_INSECURE_COMMS";

                /// <summary>
                /// Allows the use of self-signed certificates for licensing server communication.
                /// </summary>
                public const string AllowSelfSignedCerts = "libadminsettings.LICENSING_SERVER_ALLOW_SELF_SIGNED_CERTS";

                /// <summary>
                /// Custom client alias used for requesting licenses from the server.
                /// </summary>
                public const string ClientAlias = "libadminsettings.LICENSING_CLIENT_ALIAS";

                /// <summary>
                /// Forces an immediate license recheck.
                /// </summary>
                public const string ForceRecheck = "libadminsettings.LICENSING_FORCE_RECHECK";

                /// <summary>
                /// The time interval (in minutes) between automatic license state checks.
                /// </summary>
                public const string RecheckIntervalMinutes = "libadminsettings.LICENSING_RECHECK_INTERVAL_MINUTES";
            }

            /// <summary>
            /// Contains constants related to event log settings.
            /// </summary>
            public static class EventLog
            {
                public const string ConnectionPort = "libadminsettings.EVENT_LOG_CONNECTION_PORT";
                public const string Persistence = "libadminsettings.EVENT_LOG_PERSISTENCE";
                public const string MaxRecords = "libadminsettings.EVENT_LOG_MAX_RECORDS";
                public const string LogFilePath = "libadminsettings.EVENT_LOG_LOG_FILE_PATH";
                public const string MaxSingleFileSizeKb = "libadminsettings.EVENT_LOG_MAX_SINGLE_FILE_SIZE_KB";
                public const string MinDaysToPreserve = "libadminsettings.EVENT_LOG_MIN_DAYS_TO_PRESERVE";
            }

            /// <summary>
            /// Contains constants related to OPC diagnostics.
            /// </summary>
            public static class OpcDiagnostics
            {
                public const string Persistence = "libadminsettings.OPC_DIAGS_PERSISTENCE";
                public const string MaxRecords = "libadminsettings.OPC_DIAGS_MAX_RECORDS";
                public const string LogFilePath = "libadminsettings.OPC_DIAGS_LOG_FILE_PATH";
                public const string MaxSingleFileSizeKb = "libadminsettings.OPC_DIAGS_MAX_SINGLE_FILE_SIZE_KB";
                public const string MinDaysToPreserve = "libadminsettings.OPC_DIAGS_MIN_DAYS_TO_PRESERVE";
            }

            /// <summary>
            /// Contains constants related to communication diagnostics.
            /// </summary>
            public static class CommDiagnostics
            {
                public const string Persistence = "libadminsettings.COMM_DIAGS_PERSISTENCE";
                public const string MaxRecords = "libadminsettings.COMM_DIAGS_MAX_RECORDS";
                public const string LogFilePath = "libadminsettings.COMM_DIAGS_LOG_FILE_PATH";
                public const string MaxSingleFileSizeKb = "libadminsettings.COMM_DIAGS_MAX_SINGLE_FILE_SIZE_KB";
                public const string MinDaysToPreserve = "libadminsettings.COMM_DIAGS_MIN_DAYS_TO_PRESERVE";
            }

            /// <summary>
            /// Contains constants related to Configuration API diagnostics.
            /// </summary>
            public static class ConfigApi
            {
                public const string Persistence = "libadminsettings.CONFIG_API_PERSISTENCE";
                public const string MaxRecords = "libadminsettings.CONFIG_API_MAX_RECORDS";
                public const string LogFilePath = "libadminsettings.CONFIG_API_LOG_FILE_PATH";
                public const string MaxSingleFileSizeKb = "libadminsettings.CONFIG_API_MAX_SINGLE_FILE_SIZE_KB";
                public const string MinDaysToPreserve = "libadminsettings.CONFIG_API_MIN_DAYS_TO_PRESERVE";
            }

        }
    }
}