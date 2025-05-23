﻿using System;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a tag in a device or tag group.
    /// </summary>
    [RecursiveEndpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}", "/tag_groups/{groupName}", typeof(DeviceTagGroup), suffix: "/tags/{tagName}")]
    public class Tag : NamedEntity
    {
        /// <summary>
        /// A flag indicating if the tag is autogenerated.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool IsAutogenerated => GetDynamicProperty<bool>(Properties.NonSerialized.TagAutogenerated) == true;

        /// <summary>
        /// The address of the tag within the device. The format depends on the driver type configured.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string TagAddress
        {
            get => GetDynamicProperty<string>(Properties.Tag.Address) ?? string.Empty;
            set => SetDynamicProperty(Properties.Tag.Address, value);
        }

        /// <summary>
        /// The data type of the tag as found in the physical device.
        /// This setting affects how the communication driver reads and writes data.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? DataType
        {
            get => GetDynamicProperty<int>(Properties.Tag.DataType);
            set => SetDynamicProperty(Properties.Tag.DataType, value);
        }

        /// <summary>
        /// Defines the access mode of the tag. Determines if the tag is read-only or read/write.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ReadWriteAccess
        {
            get => GetDynamicProperty<int>(Properties.Tag.ReadWriteAccess);
            set => SetDynamicProperty(Properties.Tag.ReadWriteAccess, value);
        }

        /// <summary>
        /// The frequency, in milliseconds, at which the tag value is updated. This rate is 
        /// only used under certain conditions. See the Kepware server manuals for more information.
        /// The default is 100 milliseconds.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ScanRateMilliseconds
        {
            get => GetDynamicProperty<int>(Properties.Tag.ScanRateMilliseconds);
            set => SetDynamicProperty(Properties.Tag.ScanRateMilliseconds, value);
        }

        #region Scaling Properties

        /// <summary>
        /// Specifies the method of scaling raw values: None, Linear, or Square Root.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ScalingType
        {
            get => GetDynamicProperty<int>(Properties.Tag.ScalingType);
            set => SetDynamicProperty(Properties.Tag.ScalingType, value);
        }

        /// <summary>
        /// Defines the lower boundary of the raw data range before scaling.
        /// The valid range depends on the data type of the tag.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? ScalingRawLow
        {
            get => GetDynamicProperty<double>(Properties.Tag.ScalingRawLow);
            set => SetDynamicProperty(Properties.Tag.ScalingRawLow, value);
        }

        /// <summary>
        /// Defines the upper boundary of the raw data range before scaling.
        /// The value must be greater than ScalingRawLow.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? ScalingRawHigh
        {
            get => GetDynamicProperty<double>(Properties.Tag.ScalingRawHigh);
            set => SetDynamicProperty(Properties.Tag.ScalingRawHigh, value);
        }

        /// <summary>
        /// Defines the lower boundary of the scaled data range after applying scaling.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? ScalingScaledLow
        {
            get => GetDynamicProperty<double>(Properties.Tag.ScalingScaledLow);
            set => SetDynamicProperty(Properties.Tag.ScalingScaledLow, value);
        }

        /// <summary>
        /// Defines the upper boundary of the scaled data range after applying scaling.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? ScalingScaledHigh
        {
            get => GetDynamicProperty<double>(Properties.Tag.ScalingScaledHigh);
            set => SetDynamicProperty(Properties.Tag.ScalingScaledHigh, value);
        }

        /// <summary>
        /// Specifies the data type for the scaled value of the tag.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ScalingScaledDataType
        {
            get => GetDynamicProperty<int>(Properties.Tag.ScalingScaledDataType);
            set => SetDynamicProperty(Properties.Tag.ScalingScaledDataType, value);
        }

        /// <summary>
        /// Determines whether the scaled value is clamped to the lower boundary.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ScalingClampLow
        {
            get => GetDynamicProperty<bool>(Properties.Tag.ScalingClampLow);
            set => SetDynamicProperty(Properties.Tag.ScalingClampLow, value);
        }

        /// <summary>
        /// Determines whether the scaled value is clamped to the upper boundary.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ScalingClampHigh
        {
            get => GetDynamicProperty<bool>(Properties.Tag.ScalingClampHigh);
            set => SetDynamicProperty(Properties.Tag.ScalingClampHigh, value);
        }

        /// <summary>
        /// The engineering units associated with the scaled data.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ScalingUnits
        {
            get => GetDynamicProperty<string>(Properties.Tag.ScalingUnits);
            set => SetDynamicProperty(Properties.Tag.ScalingUnits, value);
        }

        /// <summary>
        /// If enabled, negates the scaled value before passing it to the client.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? ScalingNegateValue
        {
            get => GetDynamicProperty<bool>(Properties.Tag.ScalingNegateValue);
            set => SetDynamicProperty(Properties.Tag.ScalingNegateValue, value);
        }

        #endregion

        /// <summary>
        /// If the tag has no scaling, the scaling properties are not serialized.
        /// </summary>
        /// <returns>A set of properties to ignore when scaling is disabled.</returns>
        protected override ISet<string>? ConditionalNonSerialized()
        {
            if (GetDynamicProperty<int>(Properties.Tag.ScalingType) == 0)
            {
                return Properties.Tag.IgnoreWhenScalingDisalbedHashSet;
            }
            return null;
        }
    }
}
