using System.Text.Json.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents the collection of log groups in the DataLogger plug-in.
    /// </summary>
    [Endpoint("/config/v1/project/_datalogger/log_groups")]
    public class LogGroupCollection : EntityCollection<LogGroup>
    {
        /// <summary>Initializes a new instance of the <see cref="LogGroupCollection"/> class.</summary>
        public LogGroupCollection() { }
    }

    /// <summary>
    /// Represents the collection of log items within a log group.
    /// </summary>
    [Endpoint("/config/v1/project/_datalogger/log_groups/{logGroupName}/log_items")]
    public class LogItemCollection : EntityCollection<LogItem>
    {
        /// <summary>Initializes a new instance of the <see cref="LogItemCollection"/> class.</summary>
        public LogItemCollection() { }
    }

    /// <summary>
    /// Represents the collection of column mappings within a log group.
    /// Column mappings are auto-generated and cannot be created or deleted via the API.
    /// </summary>
    [Endpoint("/config/v1/project/_datalogger/log_groups/{logGroupName}/column_mappings")]
    public class ColumnMappingCollection : EntityCollection<ColumnMapping>
    {
        /// <summary>Initializes a new instance of the <see cref="ColumnMappingCollection"/> class.</summary>
        public ColumnMappingCollection() { }
    }

    /// <summary>
    /// Represents the collection of triggers within a log group.
    /// </summary>
    [Endpoint("/config/v1/project/_datalogger/log_groups/{logGroupName}/triggers")]
    public class TriggerCollection : EntityCollection<Trigger>
    {
        /// <summary>Initializes a new instance of the <see cref="TriggerCollection"/> class.</summary>
        public TriggerCollection() { }
    }

    // ── Intermediate group containers (serialized project format only) ──────────
    // In content=serialize JSON, children are wrapped in single-element group arrays:
    //   "log_item_groups":       [{ "common.ALLTYPES_NAME": "Log Items",       "log_items": [...] }]
    //   "column_mapping_groups": [{ "common.ALLTYPES_NAME": "Column Mappings", "column_mappings": [...] }]
    //   "trigger_groups":        [{ "common.ALLTYPES_NAME": "Triggers",        "triggers": [...] }]

    /// <summary>Intermediate container for log items in the serialized project format.</summary>
    public class LogItemGroup : NamedEntity
    {
        /// <summary>Initializes a new instance of the <see cref="LogItemGroup"/> class.</summary>
        public LogItemGroup() { }

        /// <summary>Gets or sets the log items within this group.</summary>
        [JsonPropertyName("log_items")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LogItemCollection? LogItems { get; set; }
    }

    /// <summary>Intermediate container for column mappings in the serialized project format.</summary>
    public class ColumnMappingGroup : NamedEntity
    {
        /// <summary>Initializes a new instance of the <see cref="ColumnMappingGroup"/> class.</summary>
        public ColumnMappingGroup() { }

        /// <summary>Gets or sets the column mappings within this group.</summary>
        [JsonPropertyName("column_mappings")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ColumnMappingCollection? ColumnMappings { get; set; }
    }

    /// <summary>Intermediate container for triggers in the serialized project format.</summary>
    public class TriggerGroup : NamedEntity
    {
        /// <summary>Initializes a new instance of the <see cref="TriggerGroup"/> class.</summary>
        public TriggerGroup() { }

        /// <summary>Gets or sets the triggers within this group.</summary>
        [JsonPropertyName("triggers")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TriggerCollection? Triggers { get; set; }
    }
}
