using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model.Admin
{
    public partial class AdminSettings
    {
        /// <summary>
        /// Configuration settings for the license server.
        /// Only available when the Kepware server supports licensing via license server like the TKE.
        /// </summary>
        public class AdminLicenseServerSettings(AdminSettings settings)
        {
            #region LicenseServer
            /// <summary>
            /// Host name or IP address for the license server (character limit is 63 characters).
            /// </summary>
            [YamlIgnore, JsonIgnore]
            public string? Name
            {
                get => settings.GetDynamicProperty<string>(Properties.AdminSettings.Licensing.ServerName);
                set => settings.SetDynamicProperty(Properties.AdminSettings.Licensing.ServerName, value);
            }

            /// <summary>
            /// Enables or disables the connection to the licensing server.
            /// </summary>
            [YamlIgnore, JsonIgnore]
            public bool? Enable
            {
                get => settings.GetDynamicProperty<bool>(Properties.AdminSettings.Licensing.ServerEnable);
                set => settings.SetDynamicProperty(Properties.AdminSettings.Licensing.ServerEnable, value);
            }

            /// <summary>
            /// The port number used to connect to license server for non-TLS connections
            /// </summary>
            [YamlIgnore, JsonIgnore]
            public int? Port
            {
                get => settings.GetDynamicProperty<int>(Properties.AdminSettings.Licensing.ServerPort);
                set => settings.SetDynamicProperty(Properties.AdminSettings.Licensing.ServerPort, value);
            }


            /// <summary>
            /// The port number used for TLS-secured connections to the licensing server.
            /// </summary>
            [YamlIgnore, JsonIgnore]
            public int? SslPort
            {
                get => settings.GetDynamicProperty<int>(Properties.AdminSettings.Licensing.ServerSslPort);
                set => settings.SetDynamicProperty(Properties.AdminSettings.Licensing.ServerSslPort, value);
            }

            /// <summary>
            /// Allows insecure (non-TLS) communication with the licensing server.
            /// </summary>
            [YamlIgnore, JsonIgnore]
            public bool? AllowInsecureComms
            {
                get => settings.GetDynamicProperty<bool>(Properties.AdminSettings.Licensing.AllowInsecureComms);
                set => settings.SetDynamicProperty(Properties.AdminSettings.Licensing.AllowInsecureComms, value);
            }

            /// <summary>
            /// Allows the use of self-signed certificates for licensing server communication.
            /// </summary>
            [YamlIgnore, JsonIgnore]
            public bool? AllowSelfSignedCerts
            {
                get => settings.GetDynamicProperty<bool>(Properties.AdminSettings.Licensing.AllowSelfSignedCerts);
                set => settings.SetDynamicProperty(Properties.AdminSettings.Licensing.AllowSelfSignedCerts, value);
            }

            /// <summary>
            /// Custom alias used for requesting a license from the licensing server (character limit: 63).
            /// </summary>
            [YamlIgnore, JsonIgnore]
            public string? ClientAlias
            {
                get => settings.GetDynamicProperty<string>(Properties.AdminSettings.Licensing.ClientAlias);
                set => settings.SetDynamicProperty(Properties.AdminSettings.Licensing.ClientAlias, value);
            }

            /// <summary>
            /// Time interval (in minutes) between automatic license state checks.
            /// </summary>
            [YamlIgnore, JsonIgnore]
            public int? RecheckIntervalMinutes
            {
                get => settings.GetDynamicProperty<int>(Properties.AdminSettings.Licensing.RecheckIntervalMinutes);
                set => settings.SetDynamicProperty(Properties.AdminSettings.Licensing.RecheckIntervalMinutes, value);
            }
            #endregion
        }
    }

}