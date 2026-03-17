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
            /// The driver used by this device.
            /// </summary>
            public const string DeviceDriver = "servermain.MULTIPLE_TYPES_DEVICE_DRIVER";

            /// <summary>
            /// Constant value for the key of the static tag count for the device.
            /// </summary>
            public const string StaticTagCount = Properties.NonSerialized.DeviceStaticTagCount;

        }
    }
}
