using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents a device in a channel.
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}")]
    public class Device : NamedUidEntity
    {
        /// <summary>
        /// Gets or sets the channel that owns this device.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public Channel? Channel
        {
            get => Owner as Channel;
            set => Owner = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class.
        /// </summary>
        public Device()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class with the specified name and channel.
        /// </summary>
        /// <param name="name">The name of the device.</param>
        /// <param name="channel">The channel that owns this device.</param>
        public Device(string name, Channel channel)
        {
            Name = name;
            Owner = channel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class with the specified name and channel name.
        /// </summary>
        /// <param name="name">The name of the device.</param>
        /// <param name="channelName">The name of the channel that owns this device.</param>
        public Device(string name, string channelName)
            : this(name, new Channel(channelName))
        {
        }

        /// <summary>
        /// Gets or sets the tags in the device.
        /// </summary>
        [YamlIgnore]
        [JsonPropertyName("tags")]
        [JsonPropertyOrder(100)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DeviceTagCollection? Tags { get; set; }

        /// <summary>
        /// Gets or sets the tag groups in the device.
        /// </summary>
        [YamlIgnore]
        [JsonPropertyName("tag_groups")]
        [JsonPropertyOrder(200)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DeviceTagGroupCollection? TagGroups { get; set; }

        /// <summary>
        /// Gets the driver used by this device.
        /// </summary>
        [YamlIgnore, JsonIgnore]
        public string? DeviceDriver => GetDynamicProperty<string>(Properties.Device.DeviceDriver);

        /// <summary>
        /// Gets the unique ID key for the device.
        /// </summary>
        protected override string UniqueIdKey => Properties.NonUpdatable.DeviceUniqueId;

        /// <summary>
        /// Recursively cleans up the device and all its children.
        /// </summary>
        /// <param name="defaultValueProvider">The default value provider.</param>
        /// <param name="blnRemoveProjectId">Whether to remove the project ID.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous cleanup operation.</returns>
        public override async Task Cleanup(IKepwareDefaultValueProvider defaultValueProvider, bool blnRemoveProjectId = false, CancellationToken cancellationToken = default)
        {
            await base.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);

            if (Tags != null)
            {
                foreach (var tag in Tags)
                {
                    await tag.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);
                }
            }

            if (TagGroups != null)
            {
                foreach (var tagGroup in TagGroups)
                {
                    await tagGroup.Cleanup(defaultValueProvider, blnRemoveProjectId, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
