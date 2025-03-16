using Kepware.Api.Serializer;
using System.Security.AccessControl;
using System.Text.Json;

namespace Kepware.Api.Model.Services
{
    /// <summary>
    /// Represents a promise for a job initiated on the Kepware server, allowing for asynchronous completion tracking and disposal.
    /// </summary>
    public class KepServerJobPromise : IDisposable
    {
        private readonly Task<ApiResponse<bool>> m_completionTask;
        private readonly CancellationTokenSource? m_cancellationTokenSource;
        private bool m_hasBeenDisposed = false;

        /// <summary>
        /// Gets the endpoint associated with the job.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Gets the time-to-live duration for the job.
        /// </summary>
        public TimeSpan JobTimeToLive { get; }

        /// <summary>
        /// Gets or sets the delay between completion polls.
        /// </summary>
        public TimeSpan WaitDelayBetweenCompletionPolls { get; set; } = TimeSpan.FromSeconds(1);


        /// <summary>
        /// Initializes a new instance of the <see cref="KepServerJobPromise"/> class with a job creation response message.
        /// </summary>
        /// <param name="endpoint">The endpoint associated with the job.</param>
        /// <param name="timeout">The time-to-live duration for the job.</param>
        /// <param name="jobCreationResponseMsg">The job creation response message.</param>
        /// <param name="httpClient">The HTTP client used to track the job status.</param>
        internal KepServerJobPromise(string endpoint, TimeSpan timeout, JobResponseMessage jobCreationResponseMsg, HttpClient httpClient)
        {
            Endpoint = endpoint;
            JobTimeToLive = timeout;

            if (jobCreationResponseMsg.ResponseStatus == ApiResponseCode.Accepted && jobCreationResponseMsg.JobId != null)
            {
                m_cancellationTokenSource = new();
                m_completionTask = AwaitCompletionAsync(jobCreationResponseMsg.JobId, this, httpClient, m_cancellationTokenSource.Token);
            }
            else
            {
                m_completionTask = Task.FromResult(new ApiResponse<bool>(jobCreationResponseMsg.ResponseStatus, jobCreationResponseMsg.Message ?? "Job was not accepted", endpoint));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KepServerJobPromise"/> class with a response code and message.
        /// </summary>
        /// <param name="endpoint">The endpoint associated with the job.</param>
        /// <param name="timeout">The time-to-live duration for the job.</param>
        /// <param name="responseCode">The response code indicating the job status.</param>
        /// <param name="message">The message associated with the job status.</param>
        internal KepServerJobPromise(string endpoint, TimeSpan timeout, ApiResponseCode responseCode, string message)
        {
            Endpoint = endpoint;
            JobTimeToLive = timeout;
            m_completionTask = Task.FromResult(new ApiResponse<bool>(responseCode, message, endpoint));
        }

        /// <summary>
        /// Awaits the completion of the job asynchronously.
        /// If the job is completed successfully, the task result will contain <see langword="true"/>.
        /// You can call this method multiple times to get the result, it will not re-run the job or wait again for the completion.
        /// It is safe to call this method after the job has been completed.
        /// This is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ApiResponse{T}"/> indicating the job completion status.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
        public async Task<ApiResponse<bool>> AwaitCompletionAsync(CancellationToken cancellationToken = default)
        {
            if (!m_hasBeenDisposed)
            {
                cancellationToken.Register(() => m_cancellationTokenSource?.Cancel());
                return await m_completionTask.ConfigureAwait(false);
            }
            else
            {
                throw new ObjectDisposedException(nameof(KepServerJobPromise));
            }
        }

        private static async Task<ApiResponse<bool>> AwaitCompletionAsync(string jobHref, KepServerJobPromise kepServerJobPromise, HttpClient httpClient, CancellationToken cancellationToken)
        {
            using var timeoutCts = new CancellationTokenSource(kepServerJobPromise.JobTimeToLive);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            TaskCanceledException? taskCanceledException = null;

            try
            {
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    var response = await httpClient.GetAsync(jobHref, linkedCts.Token).ConfigureAwait(false);
                    var message = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);

                    var jobStatus = JsonSerializer.Deserialize<JobStatusMessage>(message, KepJsonContext.Default.JobStatusMessage);

                    if (jobStatus?.Completed == true)
                    {
                        if (string.IsNullOrEmpty(jobStatus.Message))
                        {
                            //if the job is completed without a message, it is a success
                            return new ApiResponse<bool>(ApiResponseCode.Success, true, jobHref);
                        }
                        else
                        {
                            return new ApiResponse<bool>((ApiResponseCode)(int)response.StatusCode, jobStatus.Message, jobHref);
                        }
                    }
                    else if (!linkedCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(kepServerJobPromise.WaitDelayBetweenCompletionPolls, linkedCts.Token).ConfigureAwait(false); // Poll every second
                    }
                }
            }
            catch (TaskCanceledException tce)
            {
                // operation has been canceled
                taskCanceledException = tce;
            }

            if (timeoutCts.Token.IsCancellationRequested)
            {
                return new ApiResponse<bool>(ApiResponseCode.Timeout, "Timeout waiting for the job completion", jobHref);
            }
            else
            {
                throw (taskCanceledException ?? new TaskCanceledException());
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="KepServerJobPromise"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_hasBeenDisposed)
            {
                if (disposing)
                {
                    m_cancellationTokenSource?.Dispose();
                    m_completionTask.Dispose();
                }
                m_hasBeenDisposed = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="KepServerJobPromise"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
