﻿using Kepware.Api.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api
{
    public static class KepwareApiClientExtentions
    {
        public static async Task<Device> GetOrCreateDeviceAsync(this KepwareApiClient apiClient, Channel channel, string name, string? driverName = default, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Device name cannot be null or empty", nameof(name));

            var device = await apiClient.LoadEntityAsync<Device>(name, channel, cancellationToken: cancellationToken);

            if (device == null)
            {
                device = await apiClient.CreateDeviceAsync(channel, name, driverName, properties, cancellationToken);
                if (device != null)
                {
                    if (properties != null)
                    {
                        var currentHash = device.Hash;
                        foreach (var property in properties)
                        {
                            device.SetDynamicProperty(property.Key, property.Value);
                        }

                        if (currentHash != device.Hash)
                        {
                            await apiClient.UpdateItemAsync(device, cancellationToken: cancellationToken);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Failed to create or load device '{name}'");
                }
            }
            else
            {
                device.Tags = await apiClient.LoadCollectionAsync<DeviceTagCollection, Tag>(device, cancellationToken: cancellationToken).ConfigureAwait(false);
                device.TagGroups = await apiClient.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (device.TagGroups != null)
                {
                    await apiClient.LoadTagGroupsRecursiveAsync(device.TagGroups, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            return device;
        }

        public static async Task<Device?> CreateDeviceAsync(this KepwareApiClient apiClient, Channel channel, string name, string? driverName = default, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Device name cannot be null or empty", nameof(name));

            var device = new Device { Name = name, Owner = channel };

            driverName = driverName ?? channel.GetDynamicProperty<string>(Properties.DeviceDriver);
            device.SetDynamicProperty(Properties.DeviceDriver, driverName);

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    device.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await apiClient.InsertItemAsync<DeviceCollection, Device>(device, channel, cancellationToken: cancellationToken))
            {
                return device;
            }
            else
            {
                return null;
            }
        }

        public static async Task<Channel> GetOrCreateChannelAsync(this KepwareApiClient apiClient, string name, string driverName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(driverName))
                throw new ArgumentException("Driver name cannot be null or empty", nameof(driverName));

            var channel = await apiClient.LoadEntityAsync<Channel>(name, cancellationToken: cancellationToken);

            if (channel == null)
            {
                channel = await apiClient.CreateChannelAsync(name, driverName, properties, cancellationToken);
                if (channel != null)
                {
                    if (properties != null)
                    {
                        var currentHash = channel.Hash;
                        foreach (var property in properties)
                        {
                            channel.SetDynamicProperty(property.Key, property.Value);
                        }

                        if (currentHash != channel.Hash)
                        {
                            await apiClient.UpdateItemAsync(channel, cancellationToken: cancellationToken);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Failed to create or load channel '{name}'");
                }
            }

            return channel;
        }

        public static async Task<Channel?> CreateChannelAsync(this KepwareApiClient apiClient, string name, string driverName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(driverName))
                throw new ArgumentException("Driver name cannot be null or empty", nameof(driverName));

            var channel = new Channel { Name = name };
            channel.SetDynamicProperty(Properties.DeviceDriver, driverName);
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    channel.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await apiClient.InsertItemAsync<ChannelCollection, Channel>(channel, cancellationToken: cancellationToken))
            {
                return channel;
            }
            else
            {
                return null;
            }
        }

    }
}
