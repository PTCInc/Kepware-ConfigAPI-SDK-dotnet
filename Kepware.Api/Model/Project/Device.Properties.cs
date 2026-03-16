using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        public static class Device
        {
            /// <summary>
            /// The driver used by this channel.
            /// </summary>
            public const string DeviceDriver = "servermain.MULTIPLE_TYPES_DEVICE_DRIVER";

            /// <summary>
            /// Value of the static tag count for the channel.
            /// </summary>
            public const string StaticTagCount = "servermain.DEVICE_STATIC_TAG_COUNT";

        }
    }
}
