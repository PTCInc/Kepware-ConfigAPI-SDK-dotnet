using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model.Admin
{
    /// <summary>
    /// Defines the security modes available for an OPC UA Endpoint.
    /// </summary>
    [Flags]
    public enum UaEndpointSecurityMode
    {
        /// <summary>
        /// No security applied to the endpoint.
        /// </summary>
        None = 0,

        /// <summary>
        /// Messages are signed but not encrypted.
        /// </summary>
        Sign = 1,

        /// <summary>
        /// Messages are both signed and encrypted.
        /// </summary>
        SignAndEncrypt = 2,

        /// <summary>
        /// Allows either Sign or SignAndEncrypt.
        /// </summary>
        SignOrSignAndEncrypt = Sign | SignAndEncrypt
    }

    /// <summary>
    /// Represents an OPC UA Endpoint configuration in Kepware.
    /// </summary>
    [Endpoint("/config/v1/admin/ua_endpoints/{name}")]
    public class UaEndpoint : NamedEntity
    {
        /// <summary>
        /// Defines if the endpoint is enabled or disabled.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? Enabled
        {
            get => GetDynamicProperty<bool>(Properties.UaEndpoints.Enabled);
            set => SetDynamicProperty(Properties.UaEndpoints.Enabled, value);
        }

        /// <summary>
        /// Specifies the network adapter to which the endpoint will be bound.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Adapter
        {
            get => GetDynamicProperty<string>(Properties.UaEndpoints.Adapter);
            set => SetDynamicProperty(Properties.UaEndpoints.Adapter, value);
        }

        /// <summary>
        /// The port number to which the endpoint will be bound.
        /// </summary>
        [Range(1, 65535)]
        [YamlIgnore, JsonIgnore]
        public int? Port
        {
            get => GetDynamicProperty<int>(Properties.UaEndpoints.Port);
            set => SetDynamicProperty(Properties.UaEndpoints.Port, value);
        }

        /// <summary>
        /// The generated endpoint URL (read-only).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? Url
        {
            get => GetDynamicProperty<string>(Properties.UaEndpoints.Url);
        }


        #region Security Settings
        /// <summary>
        /// Defines if the endpoint accepts insecure connections (not recommended).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? SecurityNone
        {
            get => GetDynamicProperty<bool>(Properties.UaEndpoints.SecurityNone);
            set => SetDynamicProperty(Properties.UaEndpoints.SecurityNone, value);
        }

        /// <summary>
        /// Defines if the endpoint accepts BASIC256_SHA256 encrypted connections.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public UaEndpointSecurityMode? SecurityBasic256Sha256
        {
            get => (UaEndpointSecurityMode?)GetDynamicProperty<int>(Properties.UaEndpoints.SecurityBasic256Sha256);
            set => SetDynamicProperty(Properties.UaEndpoints.SecurityBasic256Sha256, (int?)value);
        }

        /// <summary>
        /// Defines if the endpoint accepts BASIC128_RSA15 encrypted connections (deprecated).
        /// </summary>
#pragma warning disable S1133 // Deprecated code should be removed
        [Obsolete("Deprecated in OPC UA 1.04. Use SecurityBasic256Sha256 instead.", false)]
#pragma warning restore S1133 // Deprecated code should be removed
        [YamlIgnore, JsonIgnore]
        public UaEndpointSecurityMode? SecurityBasic128Rsa15
        {
            get => (UaEndpointSecurityMode?)GetDynamicProperty<int>(Properties.UaEndpoints.SecurityBasic128Rsa15);
            set => SetDynamicProperty(Properties.UaEndpoints.SecurityBasic128Rsa15, (int?)value);
        }

        /// <summary>
        /// Defines if the endpoint accepts BASIC256 encrypted connections (deprecated).
        /// </summary>
#pragma warning disable S1133 // Deprecated code should be removed
        [Obsolete("Deprecated in OPC UA 1.04. Use SecurityBasic256Sha256 instead.", false)]
#pragma warning restore S1133 // Deprecated code should be removed
        [YamlIgnore, JsonIgnore]
        public UaEndpointSecurityMode? SecurityBasic256
        {
            get => (UaEndpointSecurityMode?)GetDynamicProperty<int?>(Properties.UaEndpoints.SecurityBasic256);
            set => SetDynamicProperty(Properties.UaEndpoints.SecurityBasic256, (int?)value);
        }

        #endregion
    }

    /// <summary>
    /// Represents a collection of OPC UA Endpoints.
    /// </summary>
    [Endpoint("/config/v1/admin/ua_endpoints")]
    public class UaEndpointCollection : EntityCollection<UaEndpoint>
    {
    }
}
