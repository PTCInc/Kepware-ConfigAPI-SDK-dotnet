using YamlDotNet.Serialization;
using System.Text.Json.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a trigger within a DataLogger log group.
    /// A trigger controls when data is logged (always, time-based, or condition-based).
    /// </summary>
    /// <remarks>
    /// The <c>events</c> (TriggerEvent) child collection is not implemented in this version
    /// as the endpoint is currently undocumented.
    /// </remarks>
    [Endpoint("/config/v1/project/_datalogger/log_groups/{logGroupName}/triggers/{name}")]
    public class Trigger : NamedEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Trigger"/> class.
        /// </summary>
        public Trigger() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Trigger"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the trigger.</param>
        public Trigger(string name) : base(name) { }

        #region General

        /// <summary>
        /// Gets or sets the trigger type (0 = Always Triggered, 1 = Time Based, 2 = Condition Based).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? TriggerType
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.TriggerType);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.TriggerType, value);
        }

        #endregion

        #region Static interval (time-based)

        /// <summary>
        /// Gets or sets whether to log data on a static time interval.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? LogOnStaticInterval
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.LogOnStaticInterval);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.LogOnStaticInterval, value);
        }

        /// <summary>
        /// Gets or sets the static interval value (in the units specified by <see cref="StaticIntervalUnits"/>).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? StaticInterval
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.StaticInterval);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.StaticInterval, value);
        }

        /// <summary>
        /// Gets or sets the units for the static interval (0 = ms, 1 = s, 2 = min, 3 = hr, 4 = days).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? StaticIntervalUnits
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.StaticIntervalUnits);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.StaticIntervalUnits, value);
        }

        #endregion

        #region Data-change (event-based)

        /// <summary>
        /// Gets or sets whether to log data only when any server item value changes.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? LogOnDataChange
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.LogOnDataChange);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.LogOnDataChange, value);
        }

        /// <summary>
        /// Gets or sets whether to log all items whenever the monitored item's value changes.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? LogAllItems
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.LogAllItems);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.LogAllItems, value);
        }

        #endregion

        #region Monitor item

        /// <summary>
        /// Gets or sets the ID of the server item used as a monitor.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? MonitorItemId
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.Trigger.MonitorItemId);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.MonitorItemId, value);
        }

        /// <summary>
        /// Gets or sets the update rate for the monitor item.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? MonitorItemUpdateRate
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.MonitorItemUpdateRate);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.MonitorItemUpdateRate, value);
        }

        /// <summary>
        /// Gets or sets the units for the monitor item update rate (0 = ms, 1 = s, 2 = min, 3 = hr, 4 = days).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? MonitorItemUpdateUnits
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.MonitorItemUpdateUnits);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.MonitorItemUpdateUnits, value);
        }

        /// <summary>
        /// Gets the data type of the monitor item (read-only; assigned by server).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? MonitorItemDataType =>
            GetDynamicProperty<string>(Properties.DataLogger.Trigger.MonitorItemDataType);

        /// <summary>
        /// Gets or sets the deadband type for the monitor item (0 = None, 1 = Absolute, 2 = Percent).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? MonitorItemDeadbandType
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.MonitorItemDeadbandType);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.MonitorItemDeadbandType, value);
        }

        /// <summary>
        /// Gets or sets the deadband value for the monitor item.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? MonitorItemDeadbandValue
        {
            get => GetDynamicProperty<double>(Properties.DataLogger.Trigger.MonitorItemDeadbandValue);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.MonitorItemDeadbandValue, value);
        }

        /// <summary>
        /// Gets or sets the lower limit of the monitor item's deadband range.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? MonitorItemDeadbandLoRange
        {
            get => GetDynamicProperty<double>(Properties.DataLogger.Trigger.MonitorItemDeadbandLoRange);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.MonitorItemDeadbandLoRange, value);
        }

        /// <summary>
        /// Gets or sets the upper limit of the monitor item's deadband range.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public double? MonitorItemDeadbandHiRange
        {
            get => GetDynamicProperty<double>(Properties.DataLogger.Trigger.MonitorItemDeadbandHiRange);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.MonitorItemDeadbandHiRange, value);
        }

        #endregion

        #region Absolute time schedule (time-based)

        /// <summary>
        /// Gets or sets the absolute time to start logging (time-based trigger).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? AbsoluteStartTime
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.Trigger.AbsoluteStartTime);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.AbsoluteStartTime, value);
        }

        /// <summary>
        /// Gets or sets the absolute time to stop logging (time-based trigger).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? AbsoluteStopTime
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.Trigger.AbsoluteStopTime);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.AbsoluteStopTime, value);
        }

        #endregion

        #region Day-of-week schedule (time-based)

        /// <summary>
        /// Gets or sets whether to enable logging on Sundays.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? DaysSunday
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.DaysSunday);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.DaysSunday, value);
        }

        /// <summary>
        /// Gets or sets whether to enable logging on Mondays.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? DaysMonday
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.DaysMonday);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.DaysMonday, value);
        }

        /// <summary>
        /// Gets or sets whether to enable logging on Tuesdays.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? DaysTuesday
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.DaysTuesday);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.DaysTuesday, value);
        }

        /// <summary>
        /// Gets or sets whether to enable logging on Wednesdays.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? DaysWednesday
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.DaysWednesday);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.DaysWednesday, value);
        }

        /// <summary>
        /// Gets or sets whether to enable logging on Thursdays.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? DaysThursday
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.DaysThursday);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.DaysThursday, value);
        }

        /// <summary>
        /// Gets or sets whether to enable logging on Fridays.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? DaysFriday
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.DaysFriday);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.DaysFriday, value);
        }

        /// <summary>
        /// Gets or sets whether to enable logging on Saturdays.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? DaysSaturday
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.DaysSaturday);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.DaysSaturday, value);
        }

        #endregion

        #region Start condition (condition-based)

        /// <summary>
        /// Gets or sets the ID of the server item controlling the start condition.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ConditionStartItemId
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.Trigger.ConditionStartItemId);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStartItemId, value);
        }

        /// <summary>
        /// Gets the data type of the start condition item (read-only; assigned by server).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ConditionStartItemDataType =>
            GetDynamicProperty<string>(Properties.DataLogger.Trigger.ConditionStartItemDataType);

        /// <summary>
        /// Gets or sets the update rate for the start condition item.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ConditionStartItemUpdateRate
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.ConditionStartItemUpdateRate);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStartItemUpdateRate, value);
        }

        /// <summary>
        /// Gets or sets the units for the start condition item update rate (0 = ms, 1 = s, 2 = min, 3 = hr, 4 = days).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ConditionStartItemUpdateUnits
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.ConditionStartItemUpdateUnits);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStartItemUpdateUnits, value);
        }

        /// <summary>
        /// Gets or sets the comparison type for the start condition.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ConditionStartConditionType
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.ConditionStartConditionType);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStartConditionType, value);
        }

        /// <summary>
        /// Gets or sets the conditional value for the start condition.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ConditionStartConditionData
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.Trigger.ConditionStartConditionData);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStartConditionData, value);
        }

        #endregion

        #region Stop condition (condition-based)

        /// <summary>
        /// Gets or sets the ID of the server item controlling the stop condition.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ConditionStopItemId
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.Trigger.ConditionStopItemId);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStopItemId, value);
        }

        /// <summary>
        /// Gets the data type of the stop condition item (read-only; assigned by server).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ConditionStopItemDataType =>
            GetDynamicProperty<string>(Properties.DataLogger.Trigger.ConditionStopItemDataType);

        /// <summary>
        /// Gets or sets the update rate for the stop condition item.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ConditionStopItemUpdateRate
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.ConditionStopItemUpdateRate);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStopItemUpdateRate, value);
        }

        /// <summary>
        /// Gets or sets the units for the stop condition item update rate (0 = ms, 1 = s, 2 = min, 3 = hr, 4 = days).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ConditionStopItemUpdateUnits
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.ConditionStopItemUpdateUnits);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStopItemUpdateUnits, value);
        }

        /// <summary>
        /// Gets or sets the comparison type for the stop condition.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? ConditionStopConditionType
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.Trigger.ConditionStopConditionType);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStopConditionType, value);
        }

        /// <summary>
        /// Gets or sets the conditional value for the stop condition.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ConditionStopConditionData
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.Trigger.ConditionStopConditionData);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.ConditionStopConditionData, value);
        }

        #endregion

        #region Log-all-items on start/stop

        /// <summary>
        /// Gets or sets whether to log all items once when the start time or condition is met.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? LogAllItemsOnStart
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.LogAllItemsOnStart);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.LogAllItemsOnStart, value);
        }

        /// <summary>
        /// Gets or sets whether to log all items once when the stop time or condition is met.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? LogAllItemsOnStop
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.Trigger.LogAllItemsOnStop);
            set => SetDynamicProperty(Properties.DataLogger.Trigger.LogAllItemsOnStop, value);
        }

        #endregion
    }
}
