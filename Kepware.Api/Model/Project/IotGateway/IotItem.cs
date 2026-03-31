using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents an IoT Item in the IoT Gateway. IoT Items are children of agent types
    /// and identify the data objects exposed by the agent.
    /// </summary>
    [Endpoint("/config/v1/project/_iot_gateway/mqtt_clients/{agentName}/iot_items/{name}")]
    public class IotItem : NamedEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IotItem"/> class.
        /// </summary>
        public IotItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotItem"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the IoT Item.</param>
        public IotItem(string name) : base(name)
        {
        }

        #region Properties

        /// <summary>
        /// Gets or sets the full channel.device.name of the referenced server tag.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ServerTag
        {
            get => GetDynamicProperty<string>(Properties.IotItem.ServerTag);
            set => SetDynamicProperty(Properties.IotItem.ServerTag, value);
        }

        /// <summary>
        /// Gets or sets whether to use scan rate to collect data from the device.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? UseScanRate
        {
            get => GetDynamicProperty<bool>(Properties.IotItem.UseScanRate);
            set => SetDynamicProperty(Properties.IotItem.UseScanRate, value);
        }

        /// <summary>
        /// Gets or sets the scan rate in milliseconds.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ScanRateMs
        {
            get => GetDynamicProperty<int>(Properties.IotItem.ScanRateMs);
            set => SetDynamicProperty(Properties.IotItem.ScanRateMs, value);
        }

        /// <summary>
        /// Gets or sets whether to publish on every scan regardless of value change.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? PublishEveryScan
        {
            get => GetDynamicProperty<bool>(Properties.IotItem.PublishEveryScan);
            set => SetDynamicProperty(Properties.IotItem.PublishEveryScan, value);
        }

        /// <summary>
        /// Gets or sets the deadband percentage for publish threshold.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? DeadbandPercent
        {
            get => GetDynamicProperty<double>(Properties.IotItem.DeadbandPercent);
            set => SetDynamicProperty(Properties.IotItem.DeadbandPercent, value);
        }

        /// <summary>
        /// Gets or sets whether the IoT Item is enabled.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? Enabled
        {
            get => GetDynamicProperty<bool>(Properties.IotItem.Enabled);
            set => SetDynamicProperty(Properties.IotItem.Enabled, value);
        }

        /// <summary>
        /// Gets or sets the data type of the referenced tag.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public IotItemDataType? DataType
        {
            get => (IotItemDataType?)GetDynamicProperty<int>(Properties.IotItem.DataType);
            set => SetDynamicProperty(Properties.IotItem.DataType, (int?)value);
        }

        #endregion
    }
}
