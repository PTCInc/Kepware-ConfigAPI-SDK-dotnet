using System.Text.Json.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Container class representing the <c>_datalogger</c> node in the full project JSON.
    /// Holds the log group collection for the DataLogger plug-in.
    /// </summary>
    public class DataLoggerContainer
    {
        /// <summary>
        /// Gets or sets the log groups managed by the DataLogger plug-in.
        /// </summary>
        [JsonPropertyName("log_groups")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LogGroupCollection? LogGroups { get; set; }

        /// <summary>
        /// Gets a value indicating whether this container holds no log groups.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => LogGroups == null || LogGroups.Count == 0;
    }
}
