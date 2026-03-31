using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Base class for IoT Gateway agent types that support publishing (MQTT Client and REST Client).
    /// Contains publish configuration and message template properties not applicable to REST Server agents.
    /// </summary>
    public class PublishingIotAgent : IotAgent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PublishingIotAgent"/> class.
        /// </summary>
        public PublishingIotAgent()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishingIotAgent"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name of the agent.</param>
        public PublishingIotAgent(string name) : base(name)
        {
        }

        #region Publish Properties

        /// <summary>
        /// Gets or sets the publish type (Interval or On Data Change).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public IotPublishType? PublishType
        {
            get => (IotPublishType?)GetDynamicProperty<int>(Properties.IotAgent.PublishType);
            set => SetDynamicProperty(Properties.IotAgent.PublishType, (int?)value);
        }

        /// <summary>
        /// Gets or sets the publish rate in milliseconds.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? RateMs
        {
            get => GetDynamicProperty<int>(Properties.IotAgent.RateMs);
            set => SetDynamicProperty(Properties.IotAgent.RateMs, value);
        }

        /// <summary>
        /// Gets or sets the publish format (Wide or Narrow).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public IotPublishFormat? PublishFormat
        {
            get => (IotPublishFormat?)GetDynamicProperty<int>(Properties.IotAgent.PublishFormat);
            set => SetDynamicProperty(Properties.IotAgent.PublishFormat, (int?)value);
        }

        /// <summary>
        /// Gets or sets the maximum number of tag events per publish in narrow format.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? MaxEventsPerPublish
        {
            get => GetDynamicProperty<int>(Properties.IotAgent.MaxEventsPerPublish);
            set => SetDynamicProperty(Properties.IotAgent.MaxEventsPerPublish, value);
        }

        /// <summary>
        /// Gets or sets the transaction timeout in seconds.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public int? TransactionTimeoutS
        {
            get => GetDynamicProperty<int>(Properties.IotAgent.TransactionTimeoutS);
            set => SetDynamicProperty(Properties.IotAgent.TransactionTimeoutS, value);
        }

        /// <summary>
        /// Gets or sets whether the initial update should be sent when the agent starts.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public bool? SendInitialUpdate
        {
            get => GetDynamicProperty<bool>(Properties.IotAgent.SendInitialUpdate);
            set => SetDynamicProperty(Properties.IotAgent.SendInitialUpdate, value);
        }

        #endregion

        #region Message Properties

        /// <summary>
        /// Gets or sets the message format (Standard or Advanced Template).
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public IotMessageFormat? MessageFormat
        {
            get => (IotMessageFormat?)GetDynamicProperty<int>(Properties.IotAgent.MessageFormat);
            set => SetDynamicProperty(Properties.IotAgent.MessageFormat, (int?)value);
        }

        /// <summary>
        /// Gets or sets the standard template for message formatting.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? StandardTemplate
        {
            get => GetDynamicProperty<string>(Properties.IotAgent.StandardTemplate);
            set => SetDynamicProperty(Properties.IotAgent.StandardTemplate, value);
        }

        /// <summary>
        /// Gets or sets the expansion of |VALUES| in the standard template.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? ExpansionOfValues
        {
            get => GetDynamicProperty<string>(Properties.IotAgent.ExpansionOfValues);
            set => SetDynamicProperty(Properties.IotAgent.ExpansionOfValues, value);
        }

        /// <summary>
        /// Gets or sets the advanced template for message formatting.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? AdvancedTemplate
        {
            get => GetDynamicProperty<string>(Properties.IotAgent.AdvancedTemplate);
            set => SetDynamicProperty(Properties.IotAgent.AdvancedTemplate, value);
        }

        #endregion
    }
}
