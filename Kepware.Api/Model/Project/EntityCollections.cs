using Kepware.Api.Serializer;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;

namespace Kepware.Api.Model
{
    /// <summary>
    /// Represents the collection of channels in a project
    /// </summary>
    [Endpoint("/config/v1/project/channels")]
    public class ChannelCollection : EntityCollection<Channel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelCollection"/> class.
        /// </summary>
        public ChannelCollection() { }

    }

    /// <summary>
    /// Represents the collection of devices in a channel
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices")]
    public class DeviceCollection : EntityCollection<Device>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceCollection"/> class.
        /// </summary>
        public DeviceCollection() { }
    }

    /// <summary>
    /// Represents the collection of tags in a device
    /// </summary>
    [RecursiveEndpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}", "/tag_groups/{groupName}", typeof(DeviceTagGroup), suffix: "/tags")]
    public class DeviceTagCollection : EntityCollection<Tag>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceTagCollection"/> class.
        /// </summary>
        public DeviceTagCollection() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceTagCollection"/> class.
        /// </summary>
        /// <param name="collection"></param>
        public DeviceTagCollection(IEnumerable<Tag> collection)
            : base(collection)
        {

        }
    }

    /// <summary>
    /// Represents the collection of consumer exchange groups in a device
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}/consumer_exchange_groups")]
    public class ConsumerExchangeGroupCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumerExchangeGroupCollection"/> class.
        /// </summary>
        public ConsumerExchangeGroupCollection() { }
    }

    /// <summary>
    /// Represents the collection of consumer exchanges in a consumer exchange group
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}/consumer_exchange_groups/{groupName}/consumer_exchanges")]
    public class ConsumerExchangeCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumerExchangeCollection"/> class.
        /// </summary>
        public ConsumerExchangeCollection() { }
    }

    /// <summary>
    /// Represents the collection of producer exchange groups in a device
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}/producer_exchange_groups")]
    public class ProducerExchangeGroupCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerExchangeGroupCollection"/> class.
        /// </summary>
        public ProducerExchangeGroupCollection() { }
    }

    /// <summary>
    /// Represents the collection of producer exchanges in a producer exchange group
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}/producer_exchange_groups/{groupName}/producer_exchanges")]
    public class ProducerExchangeCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProducerExchangeCollection"/> class.
        /// </summary>
        public ProducerExchangeCollection() { }
    }

    /// <summary>
    /// Represents the collection of omni mapping groups in a device
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}/omni_mapping_groups")]
    public class OmniMappingGroupCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OmniMappingGroupCollection"/> class.
        /// </summary>
        public OmniMappingGroupCollection() { }
    }

    /// <summary>
    /// Represents the collection of omni mappings in an omni mapping group
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}/omni_mapping_groups/{groupName}/omni_mappings")]
    public class OmniMappingCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OmniMappingCollection"/> class.
        /// </summary>
        public OmniMappingCollection() { }
    }

    /// <summary>
    /// Represents the collection of omni gas alarms in an omni mapping
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}/omni_mapping_groups/{groupName}/omni_mappings/{mappingName}/omni_gas_alarms")]
    public class OmniAlarmCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OmniAlarmCollection"/> class.
        /// </summary>
        public OmniAlarmCollection() { }
    }

    /// <summary>
    /// Represents the collection of tag groups in a device or tag group
    /// </summary>
    [RecursiveEndpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}", "/tag_groups/{groupName}", typeof(DeviceTagGroup), suffix: "/tag_groups")]
    public class DeviceTagGroupCollection : EntityCollection<DeviceTagGroup>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceTagGroupCollection"/> class.
        /// </summary>
        public DeviceTagGroupCollection() { }
    }


    /// <summary>
    /// Represents the collection of phonebooks in a channel
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/phonebooks")]
    public class PhonebookCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhonebookCollection"/> class.
        /// </summary>
        public PhonebookCollection() { }
    }

    /// <summary>
    /// Represents the collection of phone entries in a phonebook
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/phonebooks/{phonebookName}/phonelist")]
    public class PhoneEntryCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneEntryCollection"/> class.
        /// </summary>
        public PhoneEntryCollection() { }
    }

    /// <summary>
    /// Represents the collection of deviceprofiles in a device
    /// </summary>
    [Endpoint("/config/v1/project/channels/{channelName}/devices/{deviceName}/device_profiles")]
    public class DeviceProfileCollection : EntityCollection<DefaultEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProfileCollection"/> class.
        /// </summary>
        public DeviceProfileCollection() { }
    }
}
