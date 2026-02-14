using Kepware.Api.Model;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kepware.Api.ClientHandler
{
    /// <summary>
    /// Handles operations related to device configurations in the Kepware server.
    /// </summary>
    public class DeviceApiHandler
    {
        private readonly KepwareApiClient m_kepwareApiClient;
        private readonly ILogger<DeviceApiHandler> m_logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceApiHandler"/> class.
        /// </summary>
        /// <param name="kepwareApiClient">The Kepware API client.</param>
        /// <param name="logger">The logger instance.</param>
        public DeviceApiHandler(KepwareApiClient kepwareApiClient, ILogger<DeviceApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_logger = logger;
        }

        #region Devices
        #region GetOrCreateDeviceAsync
        /// <summary>
        /// Gets or creates a device with the specified name and driver in the specified channel.
        /// </summary>
        /// <param name="channel">The channel to which the device belongs.</param>
        /// <param name="name">The name of the device.</param>
        /// <param name="driverName">The name of the driver.</param>
        /// <param name="properties">The properties to set on the device.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created or loaded <see cref="Device"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the channel is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the device name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the device cannot be created or loaded.</exception>
        public async Task<Device> GetOrCreateDeviceAsync(Channel channel, string name, string? driverName = default, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Device name cannot be null or empty", nameof(name));

            var device = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Device>(name, channel, cancellationToken: cancellationToken);

            if (device == null)
            {
                device = await CreateDeviceAsync(channel, name, driverName, properties, cancellationToken);
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
                            await m_kepwareApiClient.GenericConfig.UpdateItemAsync(device, cancellationToken: cancellationToken);
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
                device.Tags = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(device, cancellationToken: cancellationToken).ConfigureAwait(false);
                device.TagGroups = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (device.TagGroups != null)
                {
                    await ProjectApiHandler.LoadTagGroupsRecursiveAsync(m_kepwareApiClient, device.TagGroups, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            return device;
        }
        #endregion

        #region GetDeviceAsync
        /// <summary>
        /// Gets or creates a device with the specified name and driver in the specified channel.
        /// </summary>
        /// <param name="channel">The channel to which the device belongs.</param>
        /// <param name="name">The name of the device.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the loaded <see cref="Device"/> or null if it does not exist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the channel is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the device name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the device cannot be created or loaded.</exception>
        public async Task<Device?> GetDeviceAsync(Channel channel, string name, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Device name cannot be null or empty", nameof(name));

            var device = await m_kepwareApiClient.GenericConfig.LoadEntityAsync<Device>(name, channel, cancellationToken: cancellationToken);

            if (device != null)
            {
                device.Tags = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagCollection, Tag>(device, cancellationToken: cancellationToken).ConfigureAwait(false);
                device.TagGroups = await m_kepwareApiClient.GenericConfig.LoadCollectionAsync<DeviceTagGroupCollection, DeviceTagGroup>(device, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (device.TagGroups != null)
                {
                    await ProjectApiHandler.LoadTagGroupsRecursiveAsync(m_kepwareApiClient, device.TagGroups, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            return device;
        }
        #endregion

        #region CreateDeviceAsync
        /// <summary>
        /// Creates a new device with the specified name and driver in the specified channel.
        /// </summary>
        /// <param name="channel">The channel to which the device belongs.</param>
        /// <param name="name">The name of the device.</param>
        /// <param name="driverName">The name of the driver.</param>
        /// <param name="properties">The properties to set on the device.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="Device"/>, or null if creation failed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the channel is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the device name is null or empty.</exception>
        public async Task<Device?> CreateDeviceAsync(Channel channel, string name, string? driverName = default, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Device name cannot be null or empty", nameof(name));

            var device = new Device { Name = name, Owner = channel };

            driverName = driverName ?? channel.GetDynamicProperty<string>(Properties.Channel.DeviceDriver);

            if (string.IsNullOrEmpty(driverName))
                throw new ArgumentException("Driver name cannot be null or empty", nameof(driverName));


            device.SetDynamicProperty(Properties.Channel.DeviceDriver, driverName);

            if (properties != null)
            {
                foreach (var property in properties)
                {
                    device.SetDynamicProperty(property.Key, property.Value);
                }
            }

            if (await m_kepwareApiClient.GenericConfig.InsertItemAsync<DeviceCollection, Device>(device, channel, cancellationToken: cancellationToken))
            {
                return device;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region UpdateDeviceAsync
        /// <summary>
        /// Updates the specified device and optionally its tags and tag groups.
        /// </summary>
        /// <param name="device">The device to update.</param>
        /// <param name="updateTagsAndTagGroups">Indicates whether to update the tags and tag groups of the device.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the update was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the device or its channel is null.</exception>
        public async Task<bool> UpdateDeviceAsync(Device device, bool updateTagsAndTagGroups = false, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(device.Channel);

            var endpoint = EndpointResolver.ResolveEndpoint<Device>(device);
            var currentDevice = await m_kepwareApiClient.GenericConfig.LoadEntityByEndpointAsync<Device>(endpoint, cancellationToken: cancellationToken).ConfigureAwait(false);

            bool blnRet = true;

            if (currentDevice != null)
            {
                if (currentDevice.Hash != device.Hash)
                {
                    blnRet = await m_kepwareApiClient.GenericConfig.UpdateItemAsync(endpoint, device, currentDevice, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    //properties are equal, check tags and tag groups
                }

                if (updateTagsAndTagGroups)
                {
                    await m_kepwareApiClient.GenericConfig.CompareAndApply<DeviceTagCollection, Tag>(device.Tags, currentDevice.Tags, device, cancellationToken: cancellationToken);

                    if (device.TagGroups != null)
                        await ProjectApiHandler.RecusivlyCompareTagGroup(m_kepwareApiClient, device.TagGroups, currentDevice.TagGroups, device, cancellationToken);
                }
            }
            else
            {
                m_logger.LogWarning("Could not update device, unable to load device {DeviceName} in channel {ChannelName}", device.Name, device.Channel.Name);
                blnRet = false;
            }
            return blnRet;
        }
        #endregion

        #region DeleteDeviceAsync
        /// <summary>
        /// Deletes the specified device.
        /// </summary>
        /// <param name="device">The device to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the device is null.</exception>
        public Task<bool> DeleteDeviceAsync(Device device, CancellationToken cancellationToken = default)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync(device, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes the device with the specified name in the specified channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to which the device belongs.</param>
        /// <param name="deviceName">The name of the device to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the deletion was successful.</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name or device name is null or empty.</exception>
        public Task<bool> DeleteDeviceAsync(string channelName, string deviceName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(channelName))
                throw new ArgumentException("Channel name cannot be null or empty", nameof(channelName));
            if (string.IsNullOrEmpty(deviceName))
                throw new ArgumentException("Device name cannot be null or empty", nameof(deviceName));
            return m_kepwareApiClient.GenericConfig.DeleteItemAsync<Device>([channelName, deviceName], cancellationToken: cancellationToken);
        }
        #endregion
        #endregion
    }
}
