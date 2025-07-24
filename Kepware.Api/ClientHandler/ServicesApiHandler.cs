using Kepware.Api.Model;
using Kepware.Api.Model.Services;
using Kepware.Api.Serializer;
using Kepware.Api.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kepware.Api.ClientHandler
{
    /// <summary>
    /// Handles operations related to services in the Kepware server.
    /// </summary>
    public class ServicesApiHandler
    {
        private const string ENDPOINT_REINITIALIZE_RUNTIME = "/config/v1/project/services/ReinitializeRuntime";
        private const string ENDPOINT_TAG_GENERATION = "/config/v1/project/channels/{channelName}/devices/{deviceName}/services/TagGeneration";

        private readonly KepwareApiClient m_kepwareApiClient;
        private readonly ILogger<ServicesApiHandler> m_logger;

        public ServicesApiHandler(KepwareApiClient kepwareApiClient, ILogger<ServicesApiHandler> logger)
        {
            m_kepwareApiClient = kepwareApiClient;
            m_logger = logger;
        }


        #region ReinitializeRuntime
        /// <summary>
        /// Initiates the Reinitialize Runtime service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="KepServerJobPromise"/>.</returns>
        public Task<KepServerJobPromise> ReinitializeRuntimeAsync(CancellationToken cancellationToken = default)
            => ReinitializeRuntimeAsync(TimeSpan.FromSeconds(30), cancellationToken);

        /// <summary>
        /// Initiates the Reinitialize Runtime service.
        /// </summary>
        /// <param name="timeToLive">The job's desired Time to Live (timeout).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="KepServerJobPromise"/>.</returns>
        public async Task<KepServerJobPromise> ReinitializeRuntimeAsync(TimeSpan timeToLive, CancellationToken cancellationToken = default)
        {
            if (timeToLive.TotalSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToLive), "Time to live must be at least 1 second");
            }
            else if (timeToLive.TotalSeconds > 300)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToLive), "Time to live must be at most 300 seconds");
            }

            var request = new ServiceInvocationRequest { Name = ServiceInvocationRequest.ReinitializeRuntimeService, TimeToLiveSeconds = (int)timeToLive.TotalSeconds };
            HttpContent httpContent = new StringContent(JsonSerializer.Serialize(request, KepJsonContext.Default.ServiceInvocationRequest), Encoding.UTF8, "application/json");

            var response = await m_kepwareApiClient.HttpClient.PutAsync(ENDPOINT_REINITIALIZE_RUNTIME, httpContent, cancellationToken).ConfigureAwait(false);

            var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new KepServerJobPromise(
                    ENDPOINT_REINITIALIZE_RUNTIME, 
                    timeToLive, 
                    (ApiResponseCode)(int)response.StatusCode, 
                    $"ReinitializeRuntimeAsync request failed with status code {(ApiResponseCode)(int)response.StatusCode} and message: {message}");
            }

            try
            {
                var jobResponse = JsonSerializer.Deserialize<JobResponseMessage>(message, KepJsonContext.Default.JobResponseMessage);

                if (jobResponse != null)
                {
                    return new KepServerJobPromise(ENDPOINT_REINITIALIZE_RUNTIME, timeToLive, jobResponse, m_kepwareApiClient.HttpClient);
                }
                else
                {
                    return new KepServerJobPromise(ENDPOINT_REINITIALIZE_RUNTIME, timeToLive, (ApiResponseCode)(int)response.StatusCode, "Failed to deserialize response message");
                }
            }
            catch (JsonException jex)
            {
                return new KepServerJobPromise(ENDPOINT_REINITIALIZE_RUNTIME, timeToLive, (ApiResponseCode)(int)response.StatusCode, jex.Message);
            }
        }

        #endregion

        #region AutomaticTagGeneration
        /// <summary>
        /// Initiates the Automatic Tag Generation service for a specified channel and device.
        /// </summary>
        /// <param name="channelName">The name of the channel.</param>
        /// <param name="deviceName">The name of the device.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="KepServerJobPromise"/>.</returns>
        /// <remarks>
        /// Jobs are automatically cleaned up after their wait time has expired. This wait time is configurable.
        /// 
        /// Note: Not all drivers support Automatic Tag Generation.
        /// Tip for TKE: If using file sources for Automatic Tag Generation files must be located in the 
        /// &lt;installation_directory&gt;/user_data directory. All files in the user_data directory must be world readable 
        /// or owned by the TKE user and group that were created during installation, by default this is tkedge.
        /// </remarks>
        public Task<KepServerJobPromise> AutomaticTagGenerationAsync(string channelName, string deviceName, CancellationToken cancellationToken = default)
            => AutomaticTagGenerationAsync(channelName, deviceName, TimeSpan.FromSeconds(30), cancellationToken);

        /// <summary>
        /// Initiates the Automatic Tag Generation service for a specified channel and device.
        /// </summary>
        /// <param name="channelName">The name of the channel.</param>
        /// <param name="deviceName">The name of the device.</param>
        /// <param name="timeToLive">The job's desired Time to Live (timeout).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="KepServerJobPromise"/>.</returns>
        /// <remarks>
        /// Jobs are automatically cleaned up after their wait time has expired. This wait time is configurable.
        /// 
        /// Note: Not all drivers support Automatic Tag Generation.
        /// Tip for TKE: If using file sources for Automatic Tag Generation files must be located in the 
        /// &lt;installation_directory&gt;/user_data directory. All files in the user_data directory must be world readable 
        /// or owned by the TKE user and group that were created during installation, by default this is tkedge.
        /// </remarks>
        public async Task<KepServerJobPromise> AutomaticTagGenerationAsync(string channelName, string deviceName, TimeSpan timeToLive, CancellationToken cancellationToken = default)
        {
            if (timeToLive.TotalSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToLive), "Time to live must be at least 1 second");
            }
            else if (timeToLive.TotalSeconds > 300)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToLive), "Time to live must be at most 300 seconds");
            }

            var endpoint = EndpointResolver.ReplacePlaceholders(ENDPOINT_TAG_GENERATION, [channelName, deviceName]);

            var request = new ServiceInvocationRequest { TimeToLiveSeconds = (int)timeToLive.TotalSeconds };
            HttpContent httpContent = new StringContent(JsonSerializer.Serialize(request, KepJsonContext.Default.ServiceInvocationRequest), Encoding.UTF8, "application/json");

            var response = await m_kepwareApiClient.HttpClient.PutAsync(endpoint, httpContent, cancellationToken).ConfigureAwait(false);

            var message = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new KepServerJobPromise(
                    endpoint,
                    timeToLive,
                    (ApiResponseCode)(int)response.StatusCode,
                    $"AutomaticTagGeneration request failed with status code {(ApiResponseCode)(int)response.StatusCode} and message: {message}");
            }

            try
            {
                var jobResponse = JsonSerializer.Deserialize<JobResponseMessage>(message, KepJsonContext.Default.JobResponseMessage);

                if (jobResponse != null)
                {
                    return new KepServerJobPromise(endpoint, timeToLive, jobResponse, m_kepwareApiClient.HttpClient);
                }
                else
                {
                    return new KepServerJobPromise(endpoint, timeToLive, (ApiResponseCode)(int)response.StatusCode, "Failed to deserialize response message");
                }
            }
            catch (JsonException jex)
            {
                return new KepServerJobPromise(endpoint, timeToLive, (ApiResponseCode)(int)response.StatusCode, jex.Message);
            }
        }
        #endregion
    }
}
