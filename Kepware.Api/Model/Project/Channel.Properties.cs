using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class Channel
        {
            /// <summary>
            /// The driver used by this channel.
            /// </summary>
            public const string DeviceDriver = "servermain.MULTIPLE_TYPES_DEVICE_DRIVER";

            /// <summary>
            /// Unique identifier for the channel.
            /// </summary>
            public const string UniqueId = "servermain.CHANNEL_UNIQUE_ID";

            /// <summary>
            /// Specifies the network adapter used for Ethernet-based communication.
            /// </summary>
            public const string EthernetNetworkAdapter = "servermain.CHANNEL_ETHERNET_COMMUNICATIONS_NETWORK_ADAPTER_STRING";

            /// <summary>
            /// Controls how non-normalized IEEE-754 floating point values are handled.
            /// </summary>
            public const string NonNormalizedFloatHandling = "servermain.CHANNEL_NON_NORMALIZED_FLOATING_POINT_HANDLING";

            /// <summary>
            /// Specifies the write optimization method used for the channel.
            /// </summary>
            public const string WriteOptimizationsMethod = "servermain.CHANNEL_WRITE_OPTIMIZATIONS_METHOD";

            /// <summary>
            /// Defines the write optimization duty cycle.
            /// </summary>
            public const string WriteOptimizationsDutyCycle = "servermain.CHANNEL_WRITE_OPTIMIZATIONS_DUTY_CYCLE";

            /// <summary>
            /// Enables or disables diagnostic capture for the channel.
            /// </summary>
            public const string DiagnosticsCapture = "servermain.CHANNEL_DIAGNOSTICS_CAPTURE";

        }
    }
}