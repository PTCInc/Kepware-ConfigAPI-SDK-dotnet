namespace Kepware.Api.Model
{
    public partial class Properties
    {
        /// <summary>
        /// Property key constants for the DataLogger plug-in.
        /// </summary>
        public static class DataLogger
        {
            /// <summary>
            /// Property key constants for Log Group entities.
            /// </summary>
            public static class LogGroup
            {
                /// <summary>Enable or disable the log group.</summary>
                public const string Enabled = "datalogger.LOG_GROUP_ENABLED";

                /// <summary>Update rate used for log items in group (in the units specified by <see cref="UpdateRateUnits"/>).</summary>
                public const string UpdateRate = "datalogger.LOG_GROUP_UPDATE_RATE_MSEC";

                /// <summary>Units for the update rate (0=ms, 1=s, 2=min, 3=hr, 4=days).</summary>
                public const string UpdateRateUnits = "datalogger.LOG_GROUP_UPDATE_RATE_UNITS";

                /// <summary>Map the numeric ID column to VARCHAR(64) instead of INTEGER.</summary>
                public const string MapNumericIdToVarchar = "datalogger.LOG_GROUP_MAP_NUMERIC_ID_TO_VARCHAR";

                /// <summary>Use local time for timestamp values (false = UTC).</summary>
                public const string UseLocalTimeForTimestamp = "datalogger.LOG_GROUP_USE_LOCAL_TIME_FOR_TIMESTAMP_INSERTS";

                /// <summary>Enable store and forward.</summary>
                public const string StoreAndForwardEnabled = "datalogger.LOG_GROUP_STORE_AND_FORWARD_ENABLED";

                /// <summary>Directory where the store-and-forward file will be created.</summary>
                public const string StoreAndForwardStorageDirectory = "datalogger.LOG_GROUP_STORE_AND_FORWARD_STORAGE_DIRECTORY";

                /// <summary>Maximum store-and-forward file size in MB (1–2047).</summary>
                public const string StoreAndForwardMaxStorageSizeMb = "datalogger.LOG_GROUP_STORE_AND_FORWARD_MAX_STORAGE_SIZE";

                /// <summary>Maximum number of records held in the row output buffer before logging (1–99999).</summary>
                public const string MaxRowBufferSize = "datalogger.LOG_GROUP_MAX_ROW_BUFFER_SIZE";

                /// <summary>Data Source Name (DSN) for the database connection.</summary>
                public const string Dsn = "datalogger.LOG_GROUP_DSN";

                /// <summary>Username for the DSN connection.</summary>
                public const string DsnUsername = "datalogger.LOG_GROUP_DSN_USERNAME";

                /// <summary>Password for the DSN connection. Participates in round-trip serialization.</summary>
                public const string DsnPassword = "datalogger.LOG_GROUP_DSN_PASSWORD";

                /// <summary>Seconds to wait when connecting to the DSN (1–99999).</summary>
                public const string DsnLoginTimeout = "datalogger.LOG_GROUP_DSN_LOGIN_TIMEOUT";

                /// <summary>Seconds to wait for a statement to execute (1–99999).</summary>
                public const string DsnQueryTimeout = "datalogger.LOG_GROUP_DSN_QUERY_TIMEOUT";

                /// <summary>How data is logged to the database (0=existing table, 1=new each start, 2=new once).</summary>
                public const string TableSelection = "datalogger.LOG_GROUP_TABLE_SELECTION";

                /// <summary>Name of the database table where records are written.</summary>
                public const string TableName = "datalogger.LOG_GROUP_TABLE_NAME";

                /// <summary>Format of the data table (0=Narrow, 1=Wide).</summary>
                public const string TableFormat = "datalogger.LOG_GROUP_TABLE_FORMAT";

                /// <summary>Server item used as a batch identifier.</summary>
                public const string BatchIdItem = "datalogger.LOG_GROUP_BATCH_ID_ITEM";

                /// <summary>Data type of the batch ID item (read-only).</summary>
                public const string BatchIdItemType = NonUpdatable.DataLoggerLogGroupBatchIdItemType;

                /// <summary>Update rate for the batch ID item.</summary>
                public const string BatchIdUpdateRate = "datalogger.LOG_GROUP_BATCH_ID_UPDATE_RATE";

                /// <summary>Units for the batch ID update rate (0=ms, 1=s, 2=min, 3=hr, 4=days).</summary>
                public const string BatchIdUpdateRateUnits = "datalogger.LOG_GROUP_BATCH_ID_UPDATE_RATE_UNITS";

                /// <summary>Automatically reset column mappings when the DSN changes.</summary>
                public const string RegenerateOnDsnChange = "datalogger.LOG_GROUP_REGENERATE_ALIAS_TABLE_ON_DSN_CHANGE";

                /// <summary>Automatically modify column mappings when the Batch ID changes.</summary>
                public const string RegenerateOnBatchIdChange = "datalogger.LOG_GROUP_REGENERATE_ALIAS_TABLE_ON_BATCH_ID_CHANGE";

                /// <summary>Automatically modify column mappings when the table name changes.</summary>
                public const string RegenerateOnTableNameChange = "datalogger.LOG_GROUP_REGENERATE_ALIAS_TABLE_ON_TABLE_NAME_CHANGE";

                /// <summary>Automatically modify column mappings when the Table Selection changes.</summary>
                public const string RegenerateOnTableSelectionChange = "datalogger.LOG_GROUP_REGENERATE_ALIAS_TABLE_ON_TABLE_SELECTION_CHANGE";
            }

            /// <summary>
            /// Property key constants for Log Item entities.
            /// </summary>
            public static class LogItem
            {
                /// <summary>Full channel.device.name of the server item to be logged.</summary>
                public const string ItemId = "datalogger.LOG_ITEM_ID";

                /// <summary>Numeric ID of the server item to be logged.</summary>
                public const string NumericId = "datalogger.LOG_ITEM_NUMERIC_ID";

                /// <summary>Data type of the server item (read-only).</summary>
                public const string DataType = NonUpdatable.DataLoggerLogItemDataType;

                /// <summary>Deadband type (0=None, 1=Absolute, 2=Percent).</summary>
                public const string DeadbandType = "datalogger.LOG_ITEM_DEADBAND_TYPE";

                /// <summary>Deadband value threshold.</summary>
                public const string DeadbandValue = "datalogger.LOG_ITEM_DEADBAND_VALUE";

                /// <summary>Lower limit of the deadband range.</summary>
                public const string DeadbandLoRange = "datalogger.LOG_ITEM_DEADBAND_LO_RANGE";

                /// <summary>Upper limit of the deadband range.</summary>
                public const string DeadbandHiRange = "datalogger.LOG_ITEM_DEADBAND_HI_RANGE";
            }

            /// <summary>
            /// Property key constants for Column Mapping entities.
            /// Column mappings are auto-generated by the server and cannot be created or deleted via the API.
            /// </summary>
            public static class ColumnMapping
            {
                /// <summary>The LogItem associated with this column mapping (wide mode), or "defaultMapping" (narrow mode).</summary>
                public const string LogItemId = "datalogger.TABLE_ALIAS_LOG_ITEM_ID";

                /// <summary>Database field name for the name/string column.</summary>
                public const string FieldNameName = "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_NAME";

                /// <summary>SQL data type for the name/string column.</summary>
                public const string SqlDataTypeName = "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_NAME";

                /// <summary>Column length for the name/string column.</summary>
                public const string SqlLengthName = "datalogger.TABLE_ALIAS_SQL_LENGTH_NAME";

                /// <summary>Database field name for the numeric ID column.</summary>
                public const string FieldNameNumeric = "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_NUMERIC";

                /// <summary>SQL data type for the numeric ID column.</summary>
                public const string SqlDataTypeNumeric = "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_NUMERIC";

                /// <summary>Column length for the numeric ID column.</summary>
                public const string SqlLengthNumeric = "datalogger.TABLE_ALIAS_SQL_LENGTH_NUMERIC";

                /// <summary>Database field name for the quality column.</summary>
                public const string FieldNameQuality = "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_QUALITY";

                /// <summary>SQL data type for the quality column.</summary>
                public const string SqlDataTypeQuality = "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_QUALITY";

                /// <summary>Column length for the quality column.</summary>
                public const string SqlLengthQuality = "datalogger.TABLE_ALIAS_SQL_LENGTH_QUALITY";

                /// <summary>Database field name for the timestamp column.</summary>
                public const string FieldNameTimestamp = "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_TIMESTAMP";

                /// <summary>SQL data type for the timestamp column.</summary>
                public const string SqlDataTypeTimestamp = "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_TIMESTAMP";

                /// <summary>Column length for the timestamp column.</summary>
                public const string SqlLengthTimestamp = "datalogger.TABLE_ALIAS_SQL_LENGTH_TIMESTAMP";

                /// <summary>Database field name for the value column.</summary>
                public const string FieldNameValue = "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_VALUE";

                /// <summary>SQL data type for the value column.</summary>
                public const string SqlDataTypeValue = "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_VALUE";

                /// <summary>Column length for the value column.</summary>
                public const string SqlLengthValue = "datalogger.TABLE_ALIAS_SQL_LENGTH_VALUE";

                /// <summary>Database field name for the batch ID column.</summary>
                public const string FieldNameBatchId = "datalogger.TABLE_ALIAS_DATABASE_FIELD_NAME_BATCHID";

                /// <summary>SQL data type for the batch ID column.</summary>
                public const string SqlDataTypeBatchId = "datalogger.TABLE_ALIAS_SQL_DATA_TYPE_BATCHID";

                /// <summary>Column length for the batch ID column.</summary>
                public const string SqlLengthBatchId = "datalogger.TABLE_ALIAS_SQL_LENGTH_BATCHID";
            }

            /// <summary>
            /// Property key constants for Trigger entities.
            /// </summary>
            public static class Trigger
            {
                /// <summary>Trigger type (0=Always Triggered, 1=Time Based, 2=Condition Based).</summary>
                public const string TriggerType = "datalogger.TRIGGER_TYPE";

                /// <summary>Log data on a static time interval.</summary>
                public const string LogOnStaticInterval = "datalogger.TRIGGER_LOG_ON_STATIC_INTERVAL";

                /// <summary>Static interval value (in the units specified by <see cref="StaticIntervalUnits"/>).</summary>
                public const string StaticInterval = "datalogger.TRIGGER_STATIC_INTERVAL";

                /// <summary>Units for the static interval (0=ms, 1=s, 2=min, 3=hr, 4=days).</summary>
                public const string StaticIntervalUnits = "datalogger.TRIGGER_STATIC_INTERVAL_UNITS";

                /// <summary>Log data only when any server item value changes.</summary>
                public const string LogOnDataChange = "datalogger.TRIGGER_LOG_ON_DATA_CHANGE";

                /// <summary>Log all items whenever the monitored item's value changes.</summary>
                public const string LogAllItems = "datalogger.TRIGGER_LOG_ALL_ITEMS";

                /// <summary>ID of the server item used as a monitor.</summary>
                public const string MonitorItemId = "datalogger.TRIGGER_MONITOR_ITEM_ID";

                /// <summary>Update rate for the monitor item.</summary>
                public const string MonitorItemUpdateRate = "datalogger.TRIGGER_MONITOR_ITEM_UPDATE_RATE";

                /// <summary>Units for the monitor item update rate (0=ms, 1=s, 2=min, 3=hr, 4=days).</summary>
                public const string MonitorItemUpdateUnits = "datalogger.TRIGGER_MONITOR_ITEM_UPDATE_UNITS";

                /// <summary>Data type of the monitor item (read-only).</summary>
                public const string MonitorItemDataType = NonUpdatable.DataLoggerTriggerMonitorItemDataType;

                /// <summary>Deadband type for the monitor item (0=None, 1=Absolute, 2=Percent).</summary>
                public const string MonitorItemDeadbandType = "datalogger.TRIGGER_MONITOR_ITEM_DEADBAND_TYPE";

                /// <summary>Deadband value for the monitor item.</summary>
                public const string MonitorItemDeadbandValue = "datalogger.TRIGGER_MONITOR_ITEM_DEADBAND_VALUE";

                /// <summary>Lower limit of the monitor item's deadband range.</summary>
                public const string MonitorItemDeadbandLoRange = "datalogger.TRIGGER_MONITOR_ITEM_DEADBAND_LO_RANGE";

                /// <summary>Upper limit of the monitor item's deadband range.</summary>
                public const string MonitorItemDeadbandHiRange = "datalogger.TRIGGER_MONITOR_ITEM_DEADBAND_HI_RANGE";

                /// <summary>Absolute time to start logging (time-based trigger).</summary>
                public const string AbsoluteStartTime = "datalogger.TRIGGER_ABSOLUTE_START_TIME";

                /// <summary>Absolute time to stop logging (time-based trigger).</summary>
                public const string AbsoluteStopTime = "datalogger.TRIGGER_ABSOLUTE_STOP_TIME";

                /// <summary>Enable logging on Sundays (time-based trigger).</summary>
                public const string DaysSunday = "datalogger.TRIGGER_DAYS_SUNDAY";

                /// <summary>Enable logging on Mondays (time-based trigger).</summary>
                public const string DaysMonday = "datalogger.TRIGGER_DAYS_MONDAY";

                /// <summary>Enable logging on Tuesdays (time-based trigger).</summary>
                public const string DaysTuesday = "datalogger.TRIGGER_DAYS_TUESDAY";

                /// <summary>Enable logging on Wednesdays (time-based trigger).</summary>
                public const string DaysWednesday = "datalogger.TRIGGER_DAYS_WEDNESDAY";

                /// <summary>Enable logging on Thursdays (time-based trigger).</summary>
                public const string DaysThursday = "datalogger.TRIGGER_DAYS_THURSDAY";

                /// <summary>Enable logging on Fridays (time-based trigger).</summary>
                public const string DaysFriday = "datalogger.TRIGGER_DAYS_FRIDAY";

                /// <summary>Enable logging on Saturdays (time-based trigger).</summary>
                public const string DaysSaturday = "datalogger.TRIGGER_DAYS_SATURDAY";

                /// <summary>ID of the server item controlling the start condition.</summary>
                public const string ConditionStartItemId = "datalogger.TRIGGER_CONDITION_START_ITEM_ID";

                /// <summary>Data type of the start condition item (read-only).</summary>
                public const string ConditionStartItemDataType = NonUpdatable.DataLoggerTriggerConditionStartItemDataType;

                /// <summary>Update rate for the start condition item.</summary>
                public const string ConditionStartItemUpdateRate = "datalogger.TRIGGER_CONDITION_START_ITEM_UPDATE_RATE";

                /// <summary>Units for the start condition item update rate (0=ms, 1=s, 2=min, 3=hr, 4=days).</summary>
                public const string ConditionStartItemUpdateUnits = "datalogger.TRIGGER_CONDITION_START_ITEM_UPDATE_UNITS";

                /// <summary>Comparison type for the start condition.</summary>
                public const string ConditionStartConditionType = "datalogger.TRIGGER_CONDITION_START_CONDITION_TYPE";

                /// <summary>Conditional value for the start condition.</summary>
                public const string ConditionStartConditionData = "datalogger.TRIGGER_CONDITION_START_CONDITION_DATA";

                /// <summary>ID of the server item controlling the stop condition.</summary>
                public const string ConditionStopItemId = "datalogger.TRIGGER_CONDITION_STOP_ITEM_ID";

                /// <summary>Data type of the stop condition item (read-only).</summary>
                public const string ConditionStopItemDataType = NonUpdatable.DataLoggerTriggerConditionStopItemDataType;

                /// <summary>Update rate for the stop condition item.</summary>
                public const string ConditionStopItemUpdateRate = "datalogger.TRIGGER_CONDITION_STOP_ITEM_UPDATE_RATE";

                /// <summary>Units for the stop condition item update rate (0=ms, 1=s, 2=min, 3=hr, 4=days).</summary>
                public const string ConditionStopItemUpdateUnits = "datalogger.TRIGGER_CONDITION_STOP_ITEM_UPDATE_UNITS";

                /// <summary>Comparison type for the stop condition.</summary>
                public const string ConditionStopConditionType = "datalogger.TRIGGER_CONDITION_STOP_CONDITION_TYPE";

                /// <summary>Conditional value for the stop condition.</summary>
                public const string ConditionStopConditionData = "datalogger.TRIGGER_CONDITION_STOP_CONDITION_DATA";

                /// <summary>Log all items once when the start time or condition is met.</summary>
                public const string LogAllItemsOnStart = "datalogger.TRIGGER_ABSOLUTE_LOG_ALL_ITEMS_START";

                /// <summary>Log all items once when the stop time or condition is met.</summary>
                public const string LogAllItemsOnStop = "datalogger.TRIGGER_ABSOLUTE_LOG_ALL_ITEMS_STOP";
            }
        }
    }
}
