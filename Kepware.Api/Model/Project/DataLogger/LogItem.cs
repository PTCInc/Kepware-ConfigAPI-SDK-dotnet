using YamlDotNet.Serialization;
using System.Text.Json.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a log item within a DataLogger log group.
    /// A log item maps a server tag to a column in the database log table.
    /// </summary>
    [Endpoint("/config/v1/project/_datalogger/log_groups/{logGroupName}/log_items/{name}")]
    public class LogItem : NamedEntity
    {
        /// <summary>Initializes a new instance of the <see cref="LogItem"/> class.</summary>
        public LogItem() { }

        /// <summary>Initializes a new instance of the <see cref="LogItem"/> class with the specified name.</summary>
        /// <param name="name">The name of the log item.</param>
        public LogItem(string name) : base(name) { }

        /// <summary>
        /// Gets or sets the full channel.device.tag path of the server item to be logged.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ServerItem
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.LogItem.ItemId);
            set => SetDynamicProperty(Properties.DataLogger.LogItem.ItemId, value);
        }

        /// <summary>
        /// Gets or sets the numeric ID of the server item to be logged.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public long? NumericId
        {
            get => GetDynamicProperty<long>(Properties.DataLogger.LogItem.NumericId);
            set => SetDynamicProperty(Properties.DataLogger.LogItem.NumericId, value);
        }

        /// <summary>
        /// Gets the data type of the server item (read-only; assigned by server).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? DataType =>
            GetDynamicProperty<string>(Properties.DataLogger.LogItem.DataType);

        /// <summary>
        /// Gets or sets the deadband type (0 = None, 1 = Absolute, 2 = Percent).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? DeadbandType
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogItem.DeadbandType);
            set => SetDynamicProperty(Properties.DataLogger.LogItem.DeadbandType, value);
        }

        /// <summary>
        /// Gets or sets the deadband value threshold.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? DeadbandValue
        {
            get => GetDynamicProperty<double>(Properties.DataLogger.LogItem.DeadbandValue);
            set => SetDynamicProperty(Properties.DataLogger.LogItem.DeadbandValue, value);
        }

        /// <summary>
        /// Gets or sets the lower limit of the deadband range.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? DeadbandLoRange
        {
            get => GetDynamicProperty<double>(Properties.DataLogger.LogItem.DeadbandLoRange);
            set => SetDynamicProperty(Properties.DataLogger.LogItem.DeadbandLoRange, value);
        }

        /// <summary>
        /// Gets or sets the upper limit of the deadband range.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? DeadbandHiRange
        {
            get => GetDynamicProperty<double>(Properties.DataLogger.LogItem.DeadbandHiRange);
            set => SetDynamicProperty(Properties.DataLogger.LogItem.DeadbandHiRange, value);
        }
    }
}
