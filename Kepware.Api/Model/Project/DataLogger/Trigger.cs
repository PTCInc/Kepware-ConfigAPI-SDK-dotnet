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
        /// <summary>Initializes a new instance of the <see cref="Trigger"/> class.</summary>
        public Trigger() { }

        /// <summary>Initializes a new instance of the <see cref="Trigger"/> class with the specified name.</summary>
        /// <param name="name">The name of the trigger.</param>
        public Trigger(string name) : base(name) { }
    }
}
