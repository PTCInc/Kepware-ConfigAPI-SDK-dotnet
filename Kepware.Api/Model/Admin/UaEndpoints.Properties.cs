using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Contains constants related to OPC UA Endpoints in Kepware API.
    /// </summary>
    public partial class Properties
    {
        public static class UaEndpoints
        {
            /// <summary>
            /// Defines if the endpoint is enabled or disabled.
            /// </summary>
            public const string Enabled = "libadminsettings.UACONFIGMANAGER_ENDPOINT_ENABLE";

            /// <summary>
            /// Specifies the network adapter to which the endpoint will be bound.
            /// </summary>
            public const string Adapter = "libadminsettings.UACONFIGMANAGER_ENDPOINT_ADAPTER";

            /// <summary>
            /// The port number to which the endpoint will be bound.
            /// </summary>
            public const string Port = "libadminsettings.UACONFIGMANAGER_ENDPOINT_PORT";

            /// <summary>
            /// The generated endpoint URL (read-only).
            /// </summary>
            public const string Url = "libadminsettings.UACONFIGMANAGER_ENDPOINT_URL";

            /// <summary>
            /// Defines if the endpoint accepts insecure connections (not recommended).
            /// </summary>
            public const string SecurityNone = "libadminsettings.UACONFIGMANAGER_ENDPOINT_SECURITY_NONE";

            /// <summary>
            /// Defines if the endpoint accepts BASIC256_SHA256 encrypted connections.
            /// </summary>
            public const string SecurityBasic256Sha256 = "libadminsettings.UACONFIGMANAGER_ENDPOINT_SECURITY_BASIC256_SHA256";

            /// <summary>
            /// Defines if the endpoint accepts BASIC128_RSA15 encrypted connections (deprecated).
            /// </summary>
            public const string SecurityBasic128Rsa15 = "libadminsettings.UACONFIGMANAGER_ENDPOINT_SECURITY_BASIC128_RSA15";

            /// <summary>
            /// Defines if the endpoint accepts BASIC256 encrypted connections (deprecated).
            /// </summary>
            public const string SecurityBasic256 = "libadminsettings.UACONFIGMANAGER_ENDPOINT_SECURITY_BASIC256";
        }
    }
}
