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
    }
}
