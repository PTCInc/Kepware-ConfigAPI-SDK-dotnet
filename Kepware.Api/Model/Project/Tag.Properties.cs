using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.Model
{
    public partial class Properties
    {
        /// <summary>
        /// Contains constants related to tag properties.
        /// </summary>
        public static class Tag
        {
            /// <summary>
            /// The address property key.
            /// </summary>
            public const string Address = "servermain.TAG_ADDRESS";
            /// <summary>
            /// The data type property key.
            /// </summary>
            public const string DataType = "servermain.TAG_DATA_TYPE";
            /// <summary>
            /// The read/write access property key.
            /// </summary>
            public const string ReadWriteAccess = "servermain.TAG_READ_WRITE_ACCESS";
            /// <summary>
            /// The scan rate in milliseconds property key.
            /// </summary>
            public const string ScanRateMilliseconds = "servermain.TAG_SCAN_RATE_MILLISECONDS";
            /// <summary>
            /// The scaling type property key.
            /// </summary>
            public const string ScalingType = "servermain.TAG_SCALING_TYPE";
            /// <summary>
            /// The scaling raw low property key.
            /// </summary>
            public const string ScalingRawLow = "servermain.TAG_SCALING_RAW_LOW";
            /// <summary>
            /// The scaling raw high property key.
            /// </summary>
            public const string ScalingRawHigh = "servermain.TAG_SCALING_RAW_HIGH";
            /// <summary>
            /// The scaling scaled low property key.
            /// </summary>
            public const string ScalingScaledLow = "servermain.TAG_SCALING_SCALED_LOW";
            /// <summary>
            /// The scaling scaled high property key.
            /// </summary>
            public const string ScalingScaledHigh = "servermain.TAG_SCALING_SCALED_HIGH";
            /// <summary>
            /// The scaling scaled data type property key.
            /// </summary>
            public const string ScalingScaledDataType = "servermain.TAG_SCALING_SCALED_DATA_TYPE";
            /// <summary>
            /// The scaling clamp low property key.
            /// </summary>
            public const string ScalingClampLow = "servermain.TAG_SCALING_CLAMP_LOW";
            /// <summary>
            /// The scaling clamp high property key.
            /// </summary>
            public const string ScalingClampHigh = "servermain.TAG_SCALING_CLAMP_HIGH";
            /// <summary>
            /// The scaling units property key.
            /// </summary>
            public const string ScalingUnits = "servermain.TAG_SCALING_UNITS";
            /// <summary>
            /// The scaling negate value property key.
            /// </summary>
            public const string ScalingNegateValue = "servermain.TAG_SCALING_NEGATE_VALUE";

            /// <summary>
            /// A set of properties to ignore when scaling is disabled.
            /// </summary>
            public static readonly FrozenSet<string> IgnoreWhenScalingDisalbedHashSet = new HashSet<string> {
                        ScalingScaledDataType,
                        ScalingScaledHigh,
                        ScalingScaledLow,
                        ScalingRawHigh,
                        ScalingRawLow,
                        ScalingClampHigh,
                        ScalingClampLow,
                        ScalingNegateValue,
                        ScalingUnits,
                    }.ToFrozenSet();
        }
    }
}
