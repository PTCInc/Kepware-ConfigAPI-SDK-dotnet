using System.Collections.Generic;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a DataLogger log group. A log group defines a connection to a database (via DSN),
    /// the table to write to, and contains the log items, column mappings, and triggers that control logging.
    /// </summary>
    [Endpoint("/config/v1/project/_datalogger/log_groups/{name}")]
    public class LogGroup : NamedEntity
    {
        /// <summary>Initializes a new instance of the <see cref="LogGroup"/> class.</summary>
        public LogGroup() { }

        /// <summary>Initializes a new instance of the <see cref="LogGroup"/> class with the specified name.</summary>
        /// <param name="name">The name of the log group.</param>
        public LogGroup(string name) : base(name) { }

        #region General — Configuration

        /// <summary>Gets or sets whether the log group is enabled.</summary>
        [YamlIgnore, JsonIgnore]
        public bool? Enabled
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.LogGroup.Enabled);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.Enabled, value);
        }

        /// <summary>Gets or sets the update rate value (in the units of <see cref="UpdateRateUnits"/>).</summary>
        [YamlIgnore, JsonIgnore]
        public int? UpdateRate
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.UpdateRate);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.UpdateRate, value);
        }

        /// <summary>Gets or sets the units for <see cref="UpdateRate"/> (0=ms, 1=s, 2=min, 3=hr, 4=days).</summary>
        [YamlIgnore, JsonIgnore]
        public int? UpdateRateUnits
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.UpdateRateUnits);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.UpdateRateUnits, value);
        }

        /// <summary>Gets or sets whether the numeric ID column maps to VARCHAR(64) instead of INTEGER.</summary>
        [YamlIgnore, JsonIgnore]
        public bool? MapNumericIdToVarchar
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.LogGroup.MapNumericIdToVarchar);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.MapNumericIdToVarchar, value);
        }

        /// <summary>Gets or sets whether local time is used for timestamps (false = UTC).</summary>
        [YamlIgnore, JsonIgnore]
        public bool? UseLocalTimeForTimestamp
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.LogGroup.UseLocalTimeForTimestamp);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.UseLocalTimeForTimestamp, value);
        }

        #endregion

        #region Advanced — Store and Forward

        /// <summary>Gets or sets whether store-and-forward is enabled.</summary>
        [YamlIgnore, JsonIgnore]
        public bool? StoreAndForwardEnabled
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.LogGroup.StoreAndForwardEnabled);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.StoreAndForwardEnabled, value);
        }

        /// <summary>Gets or sets the directory for the store-and-forward file.</summary>
        [YamlIgnore, JsonIgnore]
        public string? StoreAndForwardStorageDirectory
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.LogGroup.StoreAndForwardStorageDirectory);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.StoreAndForwardStorageDirectory, value);
        }

        /// <summary>Gets or sets the maximum store-and-forward file size in MB (1–2047).</summary>
        [YamlIgnore, JsonIgnore]
        public int? StoreAndForwardMaxStorageSizeMb
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.StoreAndForwardMaxStorageSizeMb);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.StoreAndForwardMaxStorageSizeMb, value);
        }

        #endregion

        #region Advanced — Memory

        /// <summary>Gets or sets the maximum number of records held in the row output buffer (1–99999).</summary>
        [YamlIgnore, JsonIgnore]
        public int? MaxRowBufferSize
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.MaxRowBufferSize);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.MaxRowBufferSize, value);
        }

        #endregion

        #region General — Data Source

        /// <summary>Gets or sets the Data Source Name (DSN) for the database connection.</summary>
        [YamlIgnore, JsonIgnore]
        public string? Dsn
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.LogGroup.Dsn);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.Dsn, value);
        }

        /// <summary>Gets or sets the username for the DSN connection.</summary>
        [YamlIgnore, JsonIgnore]
        public string? DsnUsername
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.LogGroup.DsnUsername);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.DsnUsername, value);
        }

        /// <summary>Gets or sets the password for the DSN connection.</summary>
        [YamlIgnore, JsonIgnore]
        public string? DsnPassword
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.LogGroup.DsnPassword);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.DsnPassword, value);
        }

        /// <summary>Gets or sets the login timeout in seconds (1–99999).</summary>
        [YamlIgnore, JsonIgnore]
        public int? DsnLoginTimeout
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.DsnLoginTimeout);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.DsnLoginTimeout, value);
        }

        /// <summary>Gets or sets the query timeout in seconds (1–99999).</summary>
        [YamlIgnore, JsonIgnore]
        public int? DsnQueryTimeout
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.DsnQueryTimeout);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.DsnQueryTimeout, value);
        }

        #endregion

        #region General — Table

        /// <summary>Gets or sets how data is logged (0=existing table, 1=new each start, 2=new once).</summary>
        [YamlIgnore, JsonIgnore]
        public int? TableSelection
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.TableSelection);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.TableSelection, value);
        }

        /// <summary>Gets or sets the name of the database table to write to.</summary>
        [YamlIgnore, JsonIgnore]
        public string? TableName
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.LogGroup.TableName);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.TableName, value);
        }

        /// <summary>Gets or sets the table format (0=Narrow, 1=Wide).</summary>
        [YamlIgnore, JsonIgnore]
        public int? TableFormat
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.TableFormat);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.TableFormat, value);
        }

        #endregion

        #region Advanced — Batch Identifier

        /// <summary>Gets or sets the server item used as a batch identifier.</summary>
        [YamlIgnore, JsonIgnore]
        public string? BatchIdItem
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.LogGroup.BatchIdItem);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.BatchIdItem, value);
        }

        /// <summary>Gets the data type of the batch ID item (read-only; set by the server).</summary>
        [YamlIgnore, JsonIgnore]
        public string? BatchIdItemType
        {
            get => GetDynamicProperty<string>(Properties.DataLogger.LogGroup.BatchIdItemType);
        }

        /// <summary>Gets or sets the update rate for the batch ID item.</summary>
        [YamlIgnore, JsonIgnore]
        public int? BatchIdUpdateRate
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.BatchIdUpdateRate);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.BatchIdUpdateRate, value);
        }

        /// <summary>Gets or sets the units for <see cref="BatchIdUpdateRate"/> (0=ms, 1=s, 2=min, 3=hr, 4=days).</summary>
        [YamlIgnore, JsonIgnore]
        public int? BatchIdUpdateRateUnits
        {
            get => GetDynamicProperty<int>(Properties.DataLogger.LogGroup.BatchIdUpdateRateUnits);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.BatchIdUpdateRateUnits, value);
        }

        #endregion

        #region Advanced — Regenerate Column Mapping Rules

        /// <summary>Gets or sets whether column mappings reset automatically when the DSN changes.</summary>
        [YamlIgnore, JsonIgnore]
        public bool? RegenerateOnDsnChange
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.LogGroup.RegenerateOnDsnChange);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.RegenerateOnDsnChange, value);
        }

        /// <summary>Gets or sets whether column mappings update automatically when the Batch ID changes.</summary>
        [YamlIgnore, JsonIgnore]
        public bool? RegenerateOnBatchIdChange
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.LogGroup.RegenerateOnBatchIdChange);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.RegenerateOnBatchIdChange, value);
        }

        /// <summary>Gets or sets whether column mappings update automatically when the table name changes.</summary>
        [YamlIgnore, JsonIgnore]
        public bool? RegenerateOnTableNameChange
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.LogGroup.RegenerateOnTableNameChange);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.RegenerateOnTableNameChange, value);
        }

        /// <summary>Gets or sets whether column mappings update automatically when the table selection changes.</summary>
        [YamlIgnore, JsonIgnore]
        public bool? RegenerateOnTableSelectionChange
        {
            get => GetDynamicProperty<bool>(Properties.DataLogger.LogGroup.RegenerateOnTableSelectionChange);
            set => SetDynamicProperty(Properties.DataLogger.LogGroup.RegenerateOnTableSelectionChange, value);
        }

        #endregion

        #region Child Collections — REST API format

        /// <summary>Gets or sets the log items in this group (populated via REST API endpoints).</summary>
        [YamlIgnore]
        [JsonPropertyName("log_items")]
        [JsonPropertyOrder(100)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LogItemCollection? LogItems { get; set; }

        /// <summary>Gets or sets the column mappings in this group (populated via REST API endpoints).</summary>
        [YamlIgnore]
        [JsonPropertyName("column_mappings")]
        [JsonPropertyOrder(101)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ColumnMappingCollection? ColumnMappings { get; set; }

        /// <summary>Gets or sets the triggers in this group (populated via REST API endpoints).</summary>
        [YamlIgnore]
        [JsonPropertyName("triggers")]
        [JsonPropertyOrder(102)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TriggerCollection? Triggers { get; set; }

        #endregion

        #region Child Collections — Serialized project format

        /// <summary>
        /// Gets or sets the log item groups as they appear in the serialized project format
        /// (<c>content=serialize</c>). Flattened into <see cref="LogItems"/> by
        /// <c>SetOwnersFullProject</c> after loading.
        /// </summary>
        [YamlIgnore]
        [JsonPropertyName("log_item_groups")]
        [JsonPropertyOrder(110)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<LogItemGroup>? LogItemGroups { get; set; }

        /// <summary>
        /// Gets or sets the column mapping groups as they appear in the serialized project format.
        /// Flattened into <see cref="ColumnMappings"/> by <c>SetOwnersFullProject</c> after loading.
        /// </summary>
        [YamlIgnore]
        [JsonPropertyName("column_mapping_groups")]
        [JsonPropertyOrder(111)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ColumnMappingGroup>? ColumnMappingGroups { get; set; }

        /// <summary>
        /// Gets or sets the trigger groups as they appear in the serialized project format.
        /// Flattened into <see cref="Triggers"/> by <c>SetOwnersFullProject</c> after loading.
        /// </summary>
        [YamlIgnore]
        [JsonPropertyName("trigger_groups")]
        [JsonPropertyOrder(112)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<TriggerGroup>? TriggerGroups { get; set; }

        #endregion

        /// <inheritdoc/>
        public override async Task Cleanup(IKepwareDefaultValueProvider defaultValueProvider, bool blnRemoveProjectId = false, CancellationToken cancellationToken = default)
        {
            await base.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);

            if (LogItems != null)
            {
                foreach (var item in LogItems)
                    await item.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);
            }

            if (ColumnMappings != null)
            {
                foreach (var cm in ColumnMappings)
                    await cm.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);
            }

            if (Triggers != null)
            {
                foreach (var trigger in Triggers)
                    await trigger.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
